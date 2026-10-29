using BGShared.Models;

namespace BGShared.Analysis;

/// <summary>
/// 血气分析自动解读（规则算法，设计见《血气分析诊断算法设计.md》）。
/// 输入当前标本的检验结果，输出「考虑诊断」文本；只做解读，不做诊断断言。
/// </summary>
public static class AbgInterpreter
{
    // 参考范围（与 DisplayConfigManager 预置一致）
    private const double PhLow = 7.35, PhHigh = 7.45;
    private const double Pco2Low = 35, Pco2High = 45;
    private const double Hco3Low = 21.2, Hco3High = 28.3;
    private const double AgHigh = 16;
    private const double LacRef = 0.75, LacHigh = 2.0, LacSevere = 4.0;
    private const double KLow = 3.4, KHigh = 4.4, KCriticalLow = 2.5, KCriticalHigh = 6.0;
    private const double NaLow = 136, NaHigh = 145;
    private const double ClLow = 98, ClHigh = 107;
    private const double CaLow = 1.15, CaHigh = 1.33;
    private const double GluLow = 3.6, GluHigh = 5.3, GluSevere = 11.1;
    private const double OsmLow = 280, OsmHigh = 320;
    private const double THbLow = 114;
    private const double SO2Low = 94;
    private const double RespFailurePO2 = 60, Type2Pco2 = 50;

    private const string Disclaimer = "※ 本解读由规则算法自动生成，仅供临床参考";

    /// <summary>按检验结果列表生成解读文本</summary>
    public static string Interpret(IEnumerable<TestResult> results, string? sampleType)
    {
        var v = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);
        foreach (var r in results)
        {
            if (r.Value.HasValue && !v.ContainsKey(r.TestName))
                v[r.TestName] = r.Value.Value;
        }
        return Interpret(v, sampleType);
    }

    /// <summary>按 指标名→数值 字典生成解读文本</summary>
    public static string Interpret(IReadOnlyDictionary<string, double> v, string? sampleType)
    {
        // (严重程度, 文本)，最后按严重程度排序编号
        var lines = new List<(int Severity, string Text)>();

        bool nonArterial = !string.IsNullOrEmpty(sampleType) && sampleType != "动脉";

        // ---- Step 0 输入准备 ----
        bool hasPh = TryGet(v, "pH", out double ph);
        bool hasPco2 = TryGet(v, "pCO2", out double pco2);
        if (!hasPh || !hasPco2)
            return "关键指标不足（缺少 pH 或 pCO2），无法解读。";

        double hco3;
        bool hco3Computed;
        if (TryGet(v, "HCO3-", out double h))
        {
            hco3 = h;
            hco3Computed = false;
        }
        else
        {
            // Henderson-Hasselbalch 反算
            hco3 = 0.03 * pco2 * Math.Pow(10, ph - 6.1);
            hco3Computed = true;
        }

        if (nonArterial)
            lines.Add((95, "非动脉血标本，以下解读仅供参考"));
        if (hco3Computed)
            lines.Add((96, $"HCO3- 缺失，按 Henderson-Hasselbalch 公式反算值 {hco3:F1} 参与解读"));

        // ---- Step 1 内在一致性校验 ----
        double hComputed = 24 * pco2 / hco3;             // 由 pCO2/HCO3 推算 [H+]
        double hMeasured = Math.Pow(10, 9 - ph);          // 由 pH 换算 [H+]（nmol/L）
        if (Math.Abs(hComputed - hMeasured) / hMeasured > 0.10)
            lines.Add((97, "数据内在一致性可疑（pH/pCO2/HCO3- 不匹配），建议复查血气"));

        // ---- Step 2 酸碱血症 ----
        bool acidemia = ph < PhLow;
        bool alkalemia = ph > PhHigh;
        if (acidemia) lines.Add((0, $"酸血症（pH {ph:F3}）"));
        else if (alkalemia) lines.Add((0, $"碱血症（pH {ph:F3}）"));

        bool hco3Low = hco3 < Hco3Low, hco3High = hco3 > Hco3High;
        bool pco2Low = pco2 < Pco2Low, pco2High = pco2 > Pco2High;

        // ---- Step 3/4 原发紊乱与代偿 ----
        // 原发方向：酸血症看 HCO3 低还是 pCO2 高；碱血症反之；pH 正常但两者同向异常为混合型
        string compensationNote = acidemia || alkalemia ? "失代偿" : "完全代偿";

        if ((acidemia && hco3Low) || (!acidemia && !alkalemia && hco3Low && !pco2High))
        {
            // 原发代谢性酸中毒，Winter 公式
            double expLow = 1.5 * hco3 + 8 - 2, expHigh = 1.5 * hco3 + 8 + 2;
            lines.Add((1, $"代谢性酸中毒（HCO3- {hco3:F1}，原发，{compensationNote}）"));
            if (pco2 > expHigh)
                lines.Add((2, $"合并呼吸性酸中毒（实际 pCO2 {pco2:F1} 高于预期 {expLow:F1}-{expHigh:F1}，呼吸代偿不足）"));
            else if (pco2 < expLow)
                lines.Add((2, $"合并呼吸性碱中毒（实际 pCO2 {pco2:F1} 低于预期 {expLow:F1}-{expHigh:F1}）"));
            else
                lines.Add((90, $"呼吸代偿符合预期（pCO2 {pco2:F1}，预期 {expLow:F1}-{expHigh:F1}）"));
        }
        else if ((acidemia && pco2High) || (!acidemia && !alkalemia && pco2High && !hco3Low))
        {
            // 原发呼吸性酸中毒
            double acuteExp = 24 + (pco2 - 40) * 0.1;
            double chronicExp = 24 + (pco2 - 40) * 0.35;
            lines.Add((1, $"呼吸性酸中毒（pCO2 {pco2:F1}，原发，{compensationNote}）"));
            if (hco3 < acuteExp - 2)
                lines.Add((2, $"合并代谢性酸中毒（HCO3- {hco3:F1} 低于急性代偿预期 {acuteExp:F1}）"));
            else if (hco3 > chronicExp + 2)
                lines.Add((2, $"合并代谢性碱中毒（HCO3- {hco3:F1} 高于慢性代偿预期 {chronicExp:F1}）"));
            else
                lines.Add((90, $"代谢代偿在预期范围（HCO3- {hco3:F1}，预期 {acuteExp:F1}-{chronicExp:F1}）"));
        }
        else if ((alkalemia && hco3High) || (!acidemia && !alkalemia && hco3High && !pco2Low))
        {
            // 原发代谢性碱中毒
            double expLow = 0.7 * (hco3 - 24) + 40 - 5, expHigh = 0.7 * (hco3 - 24) + 40 + 5;
            lines.Add((1, $"代谢性碱中毒（HCO3- {hco3:F1}，原发，{compensationNote}）"));
            if (pco2 > expHigh)
                lines.Add((2, $"合并呼吸性酸中毒（实际 pCO2 {pco2:F1} 高于预期 {expLow:F1}-{expHigh:F1}）"));
            else if (pco2 < expLow)
                lines.Add((2, $"合并呼吸性碱中毒（实际 pCO2 {pco2:F1} 低于预期 {expLow:F1}-{expHigh:F1}）"));
        }
        else if ((alkalemia && pco2Low) || (!acidemia && !alkalemia && pco2Low && !hco3High))
        {
            // 原发呼吸性碱中毒
            double acuteExp = 24 - (40 - pco2) * 0.2;
            double chronicExp = 24 - (40 - pco2) * 0.5;
            lines.Add((1, $"呼吸性碱中毒（pCO2 {pco2:F1}，原发，{compensationNote}）"));
            if (hco3 > acuteExp + 2)
                lines.Add((2, $"合并代谢性碱中毒（HCO3- {hco3:F1} 高于急性代偿预期 {acuteExp:F1}）"));
            else if (hco3 < chronicExp - 2)
                lines.Add((2, $"合并代谢性酸中毒（HCO3- {hco3:F1} 低于慢性代偿预期 {chronicExp:F1}）"));
        }

        // ---- Step 5 AG 与 Delta Ratio ----
        double? ag = null;
        if (TryGet(v, "Anion gap", out double agVal)) ag = agVal;
        else if (TryGet(v, "Na+", out double na0) && TryGet(v, "Cl-", out double cl0))
            ag = na0 - cl0 - hco3;

        if (ag.HasValue && ag.Value > AgHigh)
        {
            string text = $"高 AG 型代谢性酸中毒特征（AG {ag.Value:F1}）";
            if (hco3 < 24)
            {
                double delta = (ag.Value - 12) / (24 - hco3);
                if (delta < 0.4) text += "，Delta Ratio < 0.4，考虑合并正常 AG 型（高氯性）代酸";
                else if (delta < 0.8) text += "，考虑高 AG 与正常 AG 混合型代酸";
                else if (delta <= 2) text += "，符合典型高 AG 型（考虑乳酸酸中毒/酮症酸中毒/肾功能不全）";
                else text += "，Delta Ratio > 2，考虑合并代谢性碱中毒";
            }
            lines.Add((3, text));
        }

        // ---- Step 6 氧合评估 ----
        if (TryGet(v, "pO2", out double po2))
        {
            if (po2 < RespFailurePO2)
            {
                string type = pco2 > Type2Pco2 ? "Ⅱ 型" : "Ⅰ 型";
                string o2Note = TryGet(v, "FIO2", out double fio2) && fio2 > 21
                    ? $"（吸氧 FIO2 {fio2:F0}% 状态下）" : "";
                lines.Add((10, $"{type}呼吸衰竭（pO2 {po2:F1} mmHg）{o2Note}"));
            }

            double? pf = null;
            if (TryGet(v, "pO2(a)/FIO2", out double pfVal)) pf = pfVal;
            else if (TryGet(v, "FIO2", out double fio) && fio > 0) pf = po2 / (fio / 100);

            if (pf.HasValue && pf.Value < 400)
            {
                string grade = pf.Value >= 300 ? "轻度" : pf.Value >= 200 ? "中度" : "重度";
                lines.Add((11, $"氧合指数下降（P/F {pf.Value:F0}，{grade}）"));
            }

            if (TryGet(v, "sO2", out double so2) && so2 < SO2Low)
                lines.Add((12, $"氧饱和度下降（sO2 {so2:F1}%）"));
        }

        // ---- Step 7.1 电解质与渗透压 ----
        if (TryGet(v, "K+", out double k))
        {
            if (k < KLow) lines.Add((30, $"低钾血症（K+ {k:F1}）{(k < KCriticalLow ? "（危急）" : "")}"));
            else if (k > KHigh)
            {
                string note = acidemia ? "，酸血症时细胞内钾外移，纠正酸碱紊乱后建议复查血钾" : "";
                lines.Add((30, $"高钾血症（K+ {k:F1}）{(k > KCriticalHigh ? "（危急）" : "")}{note}"));
            }
        }
        if (TryGet(v, "Na+", out double na))
        {
            if (na < NaLow)
            {
                string text = $"低钠血症（Na+ {na:F0}）";
                if (TryGet(v, "Glu", out double glu0) && glu0 > 10)
                {
                    double corrected = na + 1.6 * (glu0 - 5.5) / 5.5;
                    text += $"，高血糖可致稀释性低钠，校正钠约 {corrected:F0}";
                }
                lines.Add((31, text));
            }
            else if (na > NaHigh) lines.Add((31, $"高钠血症（Na+ {na:F0}）"));
        }
        if (TryGet(v, "Cl-", out double cl))
        {
            if (cl < ClLow) lines.Add((32, $"低氯血症（Cl- {cl:F0}）"));
            else if (cl > ClHigh)
            {
                string note = ag.HasValue && ag.Value <= AgHigh ? "，AG 正常，支持高氯性（正常 AG 型）代酸" : "";
                lines.Add((32, $"高氯血症（Cl- {cl:F0}）{note}"));
            }
        }
        if (TryGet(v, "Ca++", out double ca))
        {
            string phNote = "";
            if (Math.Abs(ph - 7.40) > 0.05)
            {
                double corrected = ca + 0.045 * (7.40 - ph) / 0.1;
                phNote = $"，pH 校正值约 {corrected:F2}";
            }
            if (ca < CaLow) lines.Add((33, $"低钙血症（Ca++ {ca:F2}{phNote}）"));
            else if (ca > CaHigh) lines.Add((33, $"高钙血症（Ca++ {ca:F2}{phNote}）"));
        }
        if (TryGet(v, "mOsm", out double mosm))
        {
            if (mosm < OsmLow) lines.Add((34, $"低渗透压（mOsm {mosm:F0}）"));
            else if (mosm > OsmHigh)
            {
                bool highGlu = TryGet(v, "Glu", out double glu1) && glu1 > GluSevere;
                lines.Add((34, $"高渗透压（mOsm {mosm:F0}）{(highGlu ? "，合并高血糖，提示高渗性高血糖状态可能" : "")}"));
            }
        }

        // ---- Step 7.2 其他伴随异常 ----
        if (TryGet(v, "Lac", out double lac))
        {
            if (lac >= LacSevere) lines.Add((20, $"重度高乳酸血症（Lac {lac:F1}，考虑乳酸酸中毒）"));
            else if (lac > LacHigh) lines.Add((20, $"高乳酸血症（Lac {lac:F1}）"));
            else if (lac > LacRef) lines.Add((21, $"乳酸升高（Lac {lac:F1}）"));
        }
        if (TryGet(v, "Glu", out double glu))
        {
            if (glu < GluLow) lines.Add((35, $"低血糖（Glu {glu:F1}）"));
            else if (glu > GluSevere) lines.Add((35, $"重度血糖升高（Glu {glu:F1}）"));
            else if (glu > GluHigh) lines.Add((36, $"血糖升高（Glu {glu:F1}）"));
        }
        if (TryGet(v, "tHb", out double thb) && thb < THbLow)
            lines.Add((40, $"血红蛋白降低（tHb {thb:F0} g/L）{(thb < 60 ? "（重度）" : "")}"));

        // 特殊提示；超出生理极限的数值视为可疑，不产生诊断词
        if (TryGet(v, "COHb", out double cohb) && cohb > 1.5 && cohb <= 20)
            lines.Add((41, $"COHb 升高（{cohb:F1}%），提示一氧化碳暴露可能"));
        if (TryGet(v, "MetHb", out double methb) && methb > 1.5 && methb <= 10)
            lines.Add((41, $"MetHb 升高（{methb:F1}%），提示高铁血红蛋白血症可能"));
        else if (methb > 10)
            lines.Add((98, $"MetHb {methb:F1}% 超出生理极限，数值可疑"));

        // ---- 输出 ----
        var sb = new System.Text.StringBuilder();
        var ordered = lines.OrderBy(l => l.Severity).ToList();
        if (ordered.Count == 0)
        {
            sb.AppendLine("本次血气分析未见明显异常。");
        }
        else
        {
            for (int i = 0; i < ordered.Count; i++)
                sb.AppendLine($"{i + 1}. {ordered[i].Text}");
        }
        sb.Append(Disclaimer);
        return sb.ToString();
    }

    private static bool TryGet(IReadOnlyDictionary<string, double> v, string name, out double value)
    {
        if (v.TryGetValue(name, out value)) return true;
        // 兼容大小写差异
        foreach (var kv in v)
        {
            if (kv.Key.Equals(name, StringComparison.OrdinalIgnoreCase))
            {
                value = kv.Value;
                return true;
            }
        }
        value = 0;
        return false;
    }
}
