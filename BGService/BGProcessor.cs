using BGShared.Models;
using BGShared.Utils;
using BGService.Archive;
using BGService.Data;
using BGService.Parser;
using BGService.SerialPort;

namespace BGService;

public class BGProcessor
{
    private readonly SerialPortService _serialPort;
    private readonly ISpecimenRepository _specimenRepo;
    private readonly ISystemLogRepository _logRepo;
    private readonly IServiceStatusRepository _statusRepo;
    private readonly IPatientRepository _patientRepo;
    private readonly MessageArchiver _archiver;

    public event Action<string>? OnLog;
    public event Action<Specimen>? OnNewSpecimen;

    public bool IsRunning => _serialPort.IsOpen;

    public BGProcessor(SerialPortSettings serialSettings, string archivePath,
        ISpecimenRepository specimenRepo,
        ISystemLogRepository logRepo,
        IServiceStatusRepository statusRepo,
        IPatientRepository patientRepo)
    {
        _specimenRepo = specimenRepo;
        _logRepo = logRepo;
        _statusRepo = statusRepo;
        _patientRepo = patientRepo;

        _serialPort = new SerialPortService(serialSettings);
        _serialPort.OnLog += msg => OnLog?.Invoke(msg);
        _serialPort.OnMessageReceived += HandleMessage;
        _serialPort.OnConnectionChanged += running =>
        {
            _statusRepo.Update(s => s.IsRunning = running);
        };

        _archiver = new MessageArchiver(archivePath, true, true);
    }

    public void Start()
    {
        _statusRepo.Update(s =>
        {
            s.StartTime = DateTime.Now;
            s.Version = "2.0.0";
            s.SerialPort = _serialPort.Settings.PortName;
            s.BaudRate = _serialPort.Settings.BaudRate;
            s.TotalSpecimens = _specimenRepo.GetCount();
            s.TodaySpecimens = _specimenRepo.GetTodayCount();
            s.LastHeartbeat = DateTime.Now;
        });

        try
        {
            _serialPort.Start();
            Log("血气分析服务启动");
        }
        catch (Exception ex)
        {
            Log($"串口启动失败: {ex.Message}，API服务将继续运行");
            _statusRepo.Update(s => { s.IsRunning = false; s.LastError = ex.Message; });
        }
    }

    public void Stop()
    {
        _serialPort.Stop();
        _statusRepo.Update(s => s.IsRunning = false);
        Log("血气分析服务停止");
    }

    public Specimen? InjectMessage(string message)
    {
        byte[] rawBytes = BGShared.Utils.EncodingHelper.GBK.GetBytes(message);
        return ProcessMessageInternal(message, rawBytes);
    }

    private Specimen? ProcessMessageInternal(string message, byte[] rawBytes)
    {
        try
        {
            var parsed = ASTMParser.Parse(message);

            if (string.IsNullOrEmpty(parsed.SpecimenNo))
            {
                Log("收到报文但无样本号，跳过");
                return null;
            }

            if (_specimenRepo.Exists(parsed.SpecimenNo))
            {
                Log($"样本 {parsed.SpecimenNo} 已存在，跳过");
                return null;
            }

            string archivePath = _archiver.SaveMessage(parsed, message, rawBytes);

            var specimen = MapToSpecimen(parsed, archivePath);
            // 自动匹配患者管理中的患者数据
            TryMatchPatient(specimen);
            _specimenRepo.Insert(specimen);

            _statusRepo.Update(s =>
            {
                s.TotalSpecimens++;
                s.TodaySpecimens++;
                s.LastMessageTime = DateTime.Now;
                s.LastHeartbeat = DateTime.Now;
                s.LastError = "";
            });

            Log($"新标本入库: {parsed.SpecimenNo} (患者: {specimen.PatientId} {specimen.PatientName}, 结果数: {parsed.Results.Count})");

            var fullSpecimen = _specimenRepo.GetBySpecimenId(parsed.SpecimenNo);
            if (fullSpecimen != null)
                OnNewSpecimen?.Invoke(fullSpecimen);

            return fullSpecimen;
        }
        catch (Exception ex)
        {
            LogError("处理报文失败", ex);
            _statusRepo.Update(s => s.LastError = ex.Message);
            return null;
        }
    }

    private void HandleMessage(string message, byte[] rawBytes)
    {
        ProcessMessageInternal(message, rawBytes);
    }

    private Specimen MapToSpecimen(ParsedASTMMessage parsed, string archivePath)
    {
        var now = DateTime.Now;

        var specimen = new Specimen
        {
            SpecimenId = parsed.SpecimenNo,
            PatientId = parsed.PatientId,
            PatientName = parsed.PatientName,
            SpecimenNo = parsed.SpecimenNo,
            SampleType = parsed.SampleType,
            Source = parsed.Source,
            BedNo = parsed.BedNo,
            Department = parsed.Department,
            Gender = parsed.Gender,
            OriginalPatientId = parsed.PatientId,
            TestTime = parsed.TestTime,
            ReceiveTime = now,
            Status = SpecimenStatus.Pending,
            RawMessagePath = archivePath
        };

        foreach (var r in parsed.Results)
        {
            var (min, max) = ValueParser.ParseRange(r.Range);
            specimen.Results.Add(new TestResult
            {
                TestName = r.TestName,
                TestCode = r.TestCode,
                Value = r.Value,
                RawValue = r.RawValue,
                Unit = r.Unit,
                Flag = MapFlag(r.Flag),
                MinRange = min,
                MaxRange = max,
                ResultType = r.ResultType,
                CreatedAt = now
            });
        }

        return specimen;
    }

    private static ResultFlag MapFlag(string flag)
    {
        if (string.IsNullOrEmpty(flag)) return ResultFlag.Normal;
        var f = flag.ToUpper().Trim();
        if (f.Contains('L'))
            return f.Contains("!") || f.Contains("<") ? ResultFlag.CriticalLow : ResultFlag.Low;
        if (f.Contains('H'))
            return f.Contains("!") || f.Contains(">") ? ResultFlag.CriticalHigh : ResultFlag.High;
        return ResultFlag.Normal;
    }

    private void Log(string msg)
    {
        OnLog?.Invoke(msg);
        _logRepo.Add("INFO", "service", msg);
    }

    /// <summary>
    /// 根据仪器发送的患者标识自动匹配患者管理中的患者数据。
    /// 匹配优先级：床位号 → 姓名缩写 → 病案号
    /// </summary>
    private void TryMatchPatient(Specimen specimen)
    {
        string matchKey = !string.IsNullOrEmpty(specimen.OriginalPatientId)
            ? specimen.OriginalPatientId
            : specimen.PatientId;
        if (string.IsNullOrWhiteSpace(matchKey)) return;

        try
        {
            var patient = _patientRepo.FindByIdOrBedNoOrName(matchKey.Trim());
            if (patient == null) return;

            specimen.PatientId = patient.PatientId;
            if (!string.IsNullOrEmpty(patient.PatientName)) specimen.PatientName = patient.PatientName;
            if (!string.IsNullOrEmpty(patient.BedNo)) specimen.BedNo = patient.BedNo;
            if (!string.IsNullOrEmpty(patient.Gender)) specimen.Gender = patient.Gender;
            if (!string.IsNullOrEmpty(patient.Department)) specimen.Department = patient.Department;

            Log($"标本 {specimen.SpecimenNo} 自动匹配患者: {patient.PatientName} (床号:{patient.BedNo})");
        }
        catch (Exception ex)
        {
            LogError("自动匹配患者失败", ex);
        }
    }

    private void LogError(string msg, Exception ex)
    {
        OnLog?.Invoke($"ERROR: {msg} - {ex.Message}");
        _logRepo.Add("ERROR", "service", msg, ex.ToString());
    }
}
