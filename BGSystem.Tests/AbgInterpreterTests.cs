using BGShared.Analysis;
using Xunit;

namespace BGSystem.Tests;

public class AbgInterpreterTests
{
    [Fact]
    public void Specimen8550_SevereLacticAcidosis()
    {
        // 真实库标本 8550：pH 6.953 / pCO2 22.5 / HCO3- 5.0 / AG 30.7 / Lac 23
        var v = new Dictionary<string, double>
        {
            ["pH"] = 6.953, ["pCO2"] = 22.5, ["HCO3-"] = 5.0,
            ["Na+"] = 139, ["Cl-"] = 103, ["Anion gap"] = 30.7,
            ["Lac"] = 23.0, ["K+"] = 5.3, ["Glu"] = 11.9,
            ["pO2"] = 86.1, ["FIO2"] = 21, ["sO2"] = 92.2, ["Ca++"] = 1.2
        };

        var text = AbgInterpreter.Interpret(v, "动脉");

        Assert.Contains("酸血症", text);
        Assert.Contains("代谢性酸中毒", text);
        Assert.Contains("合并呼吸性酸中毒", text);   // 实际 pCO2 22.5 高于 Winter 预期 13.5-17.5
        Assert.Contains("高 AG 型", text);
        Assert.Contains("重度高乳酸血症", text);
        Assert.Contains("高钾血症", text);
        Assert.Contains("重度血糖升高", text);
        Assert.DoesNotContain("呼吸衰竭", text);     // pO2 86 正常
    }

    [Fact]
    public void NormalReport_NoAbnormality()
    {
        var v = new Dictionary<string, double>
        {
            ["pH"] = 7.40, ["pCO2"] = 40, ["HCO3-"] = 24,
            ["Na+"] = 140, ["Cl-"] = 103, ["K+"] = 4.0, ["Ca++"] = 1.2,
            ["pO2"] = 95, ["FIO2"] = 21, ["sO2"] = 97, ["Lac"] = 0.5,
            ["Glu"] = 4.5, ["tHb"] = 140, ["mOsm"] = 290
        };

        var text = AbgInterpreter.Interpret(v, "动脉");

        Assert.Contains("未见明显异常", text);
    }

    [Fact]
    public void Type2RespiratoryFailure()
    {
        // COPD 风格：pH 7.30 / pCO2 68 / HCO3- 32 / pO2 52
        var v = new Dictionary<string, double>
        {
            ["pH"] = 7.30, ["pCO2"] = 68, ["HCO3-"] = 32.6,
            ["pO2"] = 52, ["FIO2"] = 21, ["sO2"] = 85
        };

        var text = AbgInterpreter.Interpret(v, "动脉");

        Assert.Contains("酸血症", text);
        Assert.Contains("呼吸性酸中毒", text);
        Assert.Contains("Ⅱ 型呼吸衰竭", text);
        Assert.Contains("氧饱和度下降", text);
    }

    [Fact]
    public void MissingKeyItems_CannotInterpret()
    {
        var v = new Dictionary<string, double> { ["K+"] = 4.0 };
        Assert.Contains("无法解读", AbgInterpreter.Interpret(v, "动脉"));
    }

    [Fact]
    public void VenousSample_AddsCautionNote()
    {
        var v = new Dictionary<string, double>
        {
            ["pH"] = 7.40, ["pCO2"] = 40, ["HCO3-"] = 24, ["pO2"] = 95
        };
        Assert.Contains("非动脉血标本", AbgInterpreter.Interpret(v, "静脉"));
    }
}
