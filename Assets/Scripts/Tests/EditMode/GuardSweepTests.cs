using NUnit.Framework;
using UnityEngine;

public class GuardSweepTests
{
    const float Step = 5f;   // 72 rayos

    static float[] Openness(System.Func<float, float> byAngle)
    {
        var open = new float[GuardStatic.ScanRays];
        for (int i = 0; i < open.Length; i++) open[i] = byAngle(i * Step);
        return open;
    }

    [Test]
    public void TodoAbierto_NoAdapta()
    {
        var arc = GuardStatic.ComputeOpenArc(Openness(_ => 1f), Step, 0f, 0.55f);
        Assert.IsTrue(arc.FullCircle);
    }

    [Test]
    public void TodoCerrado_ArcoNulo()
    {
        var arc = GuardStatic.ComputeOpenArc(Openness(_ => 0.1f), Step, 0f, 0.55f);
        Assert.IsFalse(arc.FullCircle);
        Assert.That(arc.HalfWidth, Is.EqualTo(0f));
    }

    [Test]
    public void GuardiaPegadoAlMuroSuperior_BarreHaciaAbajo()
    {
        // Como en Room_01: arriba (0°) y derecha (270°) hay pared cerca; izquierda y abajo, espacio.
        var open = Openness(a =>
        {
            float d = Mathf.DeltaAngle(0f, a);          // -180..180 respecto a "arriba"
            if (Mathf.Abs(d) <= 40f) return 0.4f;        // muro superior
            if (d < 0f && d > -120f) return 0.2f;        // muro a la derecha (ángulos negativos = derecha)
            return 1f;
        });
        var arc = GuardStatic.ComputeOpenArc(open, Step, 0f, 0.55f);

        Assert.IsFalse(arc.FullCircle);
        // El centro debe caer entre izquierda (90°) y abajo (180°), nunca mirando arriba.
        Assert.That(arc.Center, Is.GreaterThan(90f).And.LessThan(200f));
        Assert.That(arc.HalfWidth, Is.GreaterThan(60f));
    }

    [Test]
    public void ElArcoNoSePartePorElCeroDeLaCircunferencia()
    {
        // Abierto de 330° a 30° (pasando por 0°): un solo arco centrado en 0°.
        var open = Openness(a => Mathf.Abs(Mathf.DeltaAngle(0f, a)) <= 30f ? 1f : 0.1f);
        var arc = GuardStatic.ComputeOpenArc(open, Step, 0f, 0.55f);
        Assert.That(Mathf.Abs(Mathf.DeltaAngle(arc.Center, 0f)), Is.LessThan(3f));
        Assert.That(arc.HalfWidth, Is.EqualTo(32.5f).Within(0.01f));   // 13 rayos · 5° / 2
    }

    [Test]
    public void PrefiereElArcoQueContieneSuOrientacionAunqueHayaOtroMayor()
    {
        // Arco pequeño alrededor de 0° (preferido) y arco grande alrededor de 180°.
        var open = Openness(a =>
        {
            float d = Mathf.Abs(Mathf.DeltaAngle(0f, a));
            if (d <= 15f) return 1f;
            if (d >= 120f) return 1f;
            return 0.1f;
        });
        var arc = GuardStatic.ComputeOpenArc(open, Step, 0f, 0.55f);
        Assert.That(Mathf.Abs(Mathf.DeltaAngle(arc.Center, 0f)), Is.LessThan(3f));
    }

    [Test]
    public void SinOrientacionPreferidaAbierta_EligeElArcoMasAncho()
    {
        var open = Openness(a =>
        {
            float d = Mathf.Abs(Mathf.DeltaAngle(0f, a));
            return d >= 120f ? 1f : 0.1f;               // solo abierto alrededor de 180°
        });
        var arc = GuardStatic.ComputeOpenArc(open, Step, 0f, 0.55f);
        Assert.That(Mathf.Abs(Mathf.DeltaAngle(arc.Center, 180f)), Is.LessThan(3f));
    }

    [Test]
    public void LaSalidaSoloCuentaConElHeroeEncimaDelHueco()
    {
        var hatch = new Vector2(6f, -3f);
        Assert.IsTrue(ExitTrigger.IsOverHatch(new Vector2(6.2f, -3.3f), hatch, 0.45f));
        Assert.IsFalse(ExitTrigger.IsOverHatch(new Vector2(4.6f, -3f), hatch, 0.45f));   // donde antes disparaba el sensor
    }
}
