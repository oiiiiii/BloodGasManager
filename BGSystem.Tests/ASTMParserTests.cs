using BGService.Parser;

namespace BGSystem.Tests;

public class ASTMParserTests
{
    [Fact]
    public void Parse_EmptyMessage_ReturnsEmptyResult()
    {
        var result = ASTMParser.Parse("");

        Assert.NotNull(result);
        Assert.Equal("", result.RawMessage);
        Assert.Empty(result.Results);
    }

    [Fact]
    public void Parse_MessageWithHeader_ExtractsMessageTime()
    {
        var message = "H|\\^&|||BG_ANALYZER|||||||20240115103000";

        var result = ASTMParser.Parse(message);

        Assert.Equal(new DateTime(2024, 1, 15, 10, 30, 0), result.MessageTime);
    }

    [Fact]
    public void Parse_MessageWithPatient_ExtractsPatientInfo()
    {
        var message = "P|1||TESTID||张三^姓";

        var result = ASTMParser.Parse(message);

        Assert.Equal("TESTID", result.PatientId);
        Assert.Equal("张三", result.PatientName);
    }

    [Fact]
    public void Parse_MessageWithOrder_ExtractsSpecimenInfo()
    {
        var message = "O|1||SAMPLE001^SPECIMEN001|BED12|||||||||ART";

        var result = ASTMParser.Parse(message);

        Assert.Equal("SPECIMEN001", result.SpecimenNo);
        Assert.Equal("动脉", result.SampleType);
    }

    [Fact]
    public void Parse_MessageWithResult_ExtractsTestResult()
    {
        var message = "R|1|^^^pH|7.40";

        var result = ASTMParser.Parse(message);

        Assert.Single(result.Results);
        Assert.Equal("pH", result.Results[0].TestName);
        Assert.Equal(7.40, result.Results[0].Value);
    }

    [Fact]
    public void Parse_MessageWithAbnormalResult_ExtractsFlags()
    {
        var message = "R|1|^^^pH|7.50|||H";

        var result = ASTMParser.Parse(message);

        Assert.Equal("H", result.Results[0].Flag);
    }

    [Fact]
    public void Parse_FullASTMMessage_ExtractsAllData()
    {
        var message = "H|\\^&|||BG_ANALYZER|||||||20240115103000" + "\r" +
                      "P|1||PAT001||李四^姓" + "\r" +
                      "O|1||SAMPLE002^SPEC002|BED05|||||||||VEN" + "\r" +
                      "R|1|^^^pH|7.40||7.35-7.45|N" + "\r" +
                      "R|1|^^^pCO2|40||35-45|N" + "\r" +
                      "R|1|^^^pO2|100||80-100|N" + "\r" +
                      "L|1|N";

        var result = ASTMParser.Parse(message);

        Assert.Equal("PAT001", result.PatientId);
        Assert.Equal("李四", result.PatientName);
        Assert.Equal("SPEC002", result.SpecimenNo);
        Assert.Equal("静脉", result.SampleType);
        Assert.Equal(3, result.Results.Count);
        Assert.Equal("pH", result.Results[0].TestName);
        Assert.Equal("pCO2", result.Results[1].TestName);
        Assert.Equal("pO2", result.Results[2].TestName);
    }

    [Fact]
    public void Parse_MessageWithMissingFields_HandlesGracefully()
    {
        var message = "P|1|||";

        var result = ASTMParser.Parse(message);

        Assert.Equal("", result.PatientId);
        Assert.Equal("", result.PatientName);
    }

    [Fact]
    public void Parse_MessageWithArterialSampleType_ConvertsToChinese()
    {
        var message = "O|1||SAMPLE001^SPEC001|||||||||ART";

        var result = ASTMParser.Parse(message);

        Assert.Equal("动脉", result.SampleType);
    }

    [Fact]
    public void Parse_MessageWithVenousSampleType_ConvertsToChinese()
    {
        var message = "O|1||SAMPLE001^SPEC001|||||||||VEN";

        var result = ASTMParser.Parse(message);

        Assert.Equal("静脉", result.SampleType);
    }

    [Fact]
    public void Parse_MessageWithMixedSampleType_ConvertsToChinese()
    {
        var message = "O|1||SAMPLE001^SPEC001|||||||||MIX";

        var result = ASTMParser.Parse(message);

        Assert.Equal("混合静脉", result.SampleType);
    }

    [Fact]
    public void Parse_MessageWithInvalidSampleType_ReturnsEmpty()
    {
        var message = "O|1||SAMPLE001^SPEC001|||||||||UNKNOWN";

        var result = ASTMParser.Parse(message);

        Assert.Equal("", result.SampleType);
    }
}