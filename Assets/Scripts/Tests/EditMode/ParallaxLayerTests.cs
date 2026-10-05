using NUnit.Framework;
using UnityEngine;

public class ParallaxLayerTests
{
    const float Eps = 1e-4f;

    [Test]
    public void Factor0_LaCapaNoSeMueveConLaReferencia()
    {
        var origin = new Vector2(2f, 3f);
        var p = ParallaxLayer.Evaluate(origin, new Vector2(5f, -4f), Vector2.zero, Vector2.zero);
        Assert.That(Vector2.Distance(p, origin), Is.LessThan(Eps));
    }

    [Test]
    public void Factor1_LaCapaCopiaElDesplazamientoCompleto()
    {
        var origin = new Vector2(2f, 3f);
        var delta = new Vector2(5f, -4f);
        var p = ParallaxLayer.Evaluate(origin, delta, Vector2.one, Vector2.zero);
        Assert.That(Vector2.Distance(p, origin + delta), Is.LessThan(Eps));
    }

    [Test]
    public void FactorIntermedio_SeMueveMenosQueLaReferencia_FondoLejano()
    {
        var p = ParallaxLayer.Evaluate(Vector2.zero, new Vector2(10f, 0f), new Vector2(0.3f, 0.3f), Vector2.zero);
        Assert.That(p.x, Is.EqualTo(3f).Within(Eps));
        Assert.That(p.y, Is.EqualTo(0f).Within(Eps));
    }

    [Test]
    public void FactorNegativo_SeMueveEnSentidoContrario_PrimerPlano()
    {
        var p = ParallaxLayer.Evaluate(Vector2.zero, new Vector2(10f, 0f), new Vector2(-0.4f, -0.4f), Vector2.zero);
        Assert.That(p.x, Is.EqualTo(-4f).Within(Eps));
    }

    [Test]
    public void FactorPorEje_CadaEjeUsaSuPropiaProfundidad()
    {
        var p = ParallaxLayer.Evaluate(Vector2.zero, new Vector2(10f, 10f), new Vector2(0.1f, 0.5f), Vector2.zero);
        Assert.That(p.x, Is.EqualTo(1f).Within(Eps));
        Assert.That(p.y, Is.EqualTo(5f).Within(Eps));
    }

    [Test]
    public void Deriva_SeSumaAlResultado()
    {
        var p = ParallaxLayer.Evaluate(Vector2.zero, Vector2.zero, Vector2.one, new Vector2(0.5f, -0.25f));
        Assert.That(p.x, Is.EqualTo(0.5f).Within(Eps));
        Assert.That(p.y, Is.EqualTo(-0.25f).Within(Eps));
    }

    [Test]
    public void Wrap_SinPeriodo_DevuelveElMismoValor()
    {
        Assert.That(ParallaxLayer.Wrap(123.4f, 0f), Is.EqualTo(123.4f).Within(Eps));
        Assert.That(ParallaxLayer.Wrap(-7f, -1f), Is.EqualTo(-7f).Within(Eps));
    }

    [Test]
    public void Wrap_DentroDelIntervalo_NoCambia()
    {
        Assert.That(ParallaxLayer.Wrap(0.75f, 4f), Is.EqualTo(0.75f).Within(Eps));
        Assert.That(ParallaxLayer.Wrap(-1.9f, 4f), Is.EqualTo(-1.9f).Within(Eps));
    }

    [Test]
    public void Wrap_FueraDelIntervalo_VuelveAlMosaicoEquivalente()
    {
        // Mosaico de 4 unidades: 2.5 y -1.5 son el mismo punto visual.
        Assert.That(ParallaxLayer.Wrap(2.5f, 4f), Is.EqualTo(-1.5f).Within(Eps));
        Assert.That(ParallaxLayer.Wrap(9f, 4f), Is.EqualTo(1f).Within(Eps));
        Assert.That(ParallaxLayer.Wrap(-6f, 4f), Is.EqualTo(2f).Within(Eps));
    }

    [Test]
    public void Wrap_SiempreQuedaEnMedioPeriodo()
    {
        for (float x = -50f; x < 50f; x += 0.37f)
        {
            float w = ParallaxLayer.Wrap(x, 3f);
            Assert.That(w, Is.GreaterThanOrEqualTo(-1.5f - Eps));
            Assert.That(w, Is.LessThan(1.5f + Eps));
        }
    }

    [Test]
    public void FactorFromDepth_SueloEsCero_LejanoTiendeAUno()
    {
        Assert.That(ParallaxLayer.FactorFromDepth(0f), Is.EqualTo(0f).Within(Eps));
        Assert.That(ParallaxLayer.FactorFromDepth(1f), Is.EqualTo(0.5f).Within(Eps));
        Assert.That(ParallaxLayer.FactorFromDepth(3f), Is.EqualTo(0.75f).Within(Eps));
        Assert.That(ParallaxLayer.FactorFromDepth(999f), Is.GreaterThan(0.99f));
    }

    [Test]
    public void FactorFromDepth_PrimerPlano_EsNegativo()
    {
        Assert.That(ParallaxLayer.FactorFromDepth(-0.3f), Is.EqualTo(-0.3f / 0.7f).Within(Eps));
        Assert.That(ParallaxLayer.FactorFromDepth(-0.5f), Is.LessThan(0f));
    }

    [Test]
    public void CapaEnvuelta_TrasUnLargoRecorrido_SigueCercaDelOrigen()
    {
        // Simula 1000 unidades de recorrido de la referencia con factor 0.3 y mosaico de 4.
        var origin = new Vector2(1f, 1f);
        var p = ParallaxLayer.Evaluate(origin, new Vector2(1000f, 0f), new Vector2(0.3f, 0f), Vector2.zero);
        float wrappedX = ParallaxLayer.Wrap(p.x - origin.x, 4f) + origin.x;
        Assert.That(Mathf.Abs(wrappedX - origin.x), Is.LessThanOrEqualTo(2f + Eps));
    }
}
