using NUnit.Framework;
using UnityEngine;

public class VectorMathTests
{
    const float Eps = 1e-4f;

    static void AssertVector(Vector2 expected, Vector2 actual)
    {
        Assert.That(actual.x, Is.EqualTo(expected.x).Within(Eps), $"x de {actual}");
        Assert.That(actual.y, Is.EqualTo(expected.y).Within(Eps), $"y de {actual}");
    }

    // ---------- puntos y vectores ----------

    [Test]
    public void Direction_EsUnitarioYApuntaDelOrigenAlDestino()
    {
        var d = VectorMath.Direction(new Vector2(1f, 1f), new Vector2(4f, 5f));
        AssertVector(new Vector2(0.6f, 0.8f), d);
        Assert.That(d.magnitude, Is.EqualTo(1f).Within(Eps));
    }

    [Test]
    public void Direction_EntrePuntosIgualesEsCero()
    {
        AssertVector(Vector2.zero, VectorMath.Direction(new Vector2(2f, 3f), new Vector2(2f, 3f)));
    }

    [Test]
    public void Distance_EsLaMagnitudDelVectorQueUneLosPuntos()
    {
        Assert.That(VectorMath.Distance(new Vector2(0f, 0f), new Vector2(3f, 4f)), Is.EqualTo(5f).Within(Eps));
    }

    [Test]
    public void AngleBetween_ProductoPunto()
    {
        Assert.That(VectorMath.AngleBetween(Vector2.up, Vector2.up), Is.EqualTo(0f).Within(Eps));
        Assert.That(VectorMath.AngleBetween(Vector2.up, Vector2.right), Is.EqualTo(90f).Within(Eps));
        Assert.That(VectorMath.AngleBetween(Vector2.up, Vector2.down), Is.EqualTo(180f).Within(Eps));
        Assert.That(VectorMath.AngleBetween(Vector2.up, new Vector2(1f, 1f)), Is.EqualTo(45f).Within(Eps));
    }

    // ---------- cono de visión ----------

    [Test]
    public void IsInsideCone_DentroDelAnguloYAlcance()
    {
        Vector2 origin = new Vector2(2f, 2f);
        Assert.IsTrue(VectorMath.IsInsideCone(origin, Vector2.up, origin + new Vector2(0.5f, 3f), 30f, 5f));
    }

    [Test]
    public void IsInsideCone_FueraDelAngulo()
    {
        Vector2 origin = Vector2.zero;
        Assert.IsFalse(VectorMath.IsInsideCone(origin, Vector2.up, new Vector2(3f, 1f), 30f, 5f));
        Assert.IsFalse(VectorMath.IsInsideCone(origin, Vector2.up, new Vector2(0f, -2f), 30f, 5f));
    }

    [Test]
    public void IsInsideCone_FueraDeAlcanceAunqueElAnguloCuadre()
    {
        Assert.IsFalse(VectorMath.IsInsideCone(Vector2.zero, Vector2.up, new Vector2(0f, 6f), 30f, 5f));
    }

    [Test]
    public void IsInsideCone_ElConoRotaConElForward()
    {
        // Mirando a la derecha, un punto arriba queda fuera y uno a la derecha dentro.
        Assert.IsFalse(VectorMath.IsInsideCone(Vector2.zero, Vector2.right, new Vector2(0f, 3f), 30f, 5f));
        Assert.IsTrue(VectorMath.IsInsideCone(Vector2.zero, Vector2.right, new Vector2(3f, 0.5f), 30f, 5f));
    }

    // ---------- direcciones cardinales ----------

    [Test]
    public void ToCardinal_EjeDominanteYEmpateHorizontal()
    {
        AssertVector(Vector2.right, VectorMath.ToCardinal(new Vector2(1f, 0.4f), Vector2.down));
        AssertVector(Vector2.up, VectorMath.ToCardinal(new Vector2(0.3f, 1f), Vector2.down));
        AssertVector(Vector2.left, VectorMath.ToCardinal(new Vector2(-1f, -1f), Vector2.down));
        AssertVector(Vector2.down, VectorMath.ToCardinal(Vector2.zero, Vector2.down));
    }

    // ---------- coordenadas y ángulos ----------

    [Test]
    public void DirectionToAngle_ConvencionDelGuardia()
    {
        Assert.That(VectorMath.DirectionToAngle(Vector2.up), Is.EqualTo(0f).Within(Eps));
        Assert.That(VectorMath.DirectionToAngle(Vector2.right), Is.EqualTo(-90f).Within(Eps));
        Assert.That(VectorMath.DirectionToAngle(Vector2.left), Is.EqualTo(90f).Within(Eps));
        Assert.That(Mathf.Abs(VectorMath.DirectionToAngle(Vector2.down)), Is.EqualTo(180f).Within(Eps));
    }

    [Test]
    public void AngleToDirection_EsLaInversaDeDirectionToAngle()
    {
        foreach (var dir in new[] { Vector2.up, Vector2.right, Vector2.left, Vector2.down, new Vector2(1f, 1f).normalized })
        {
            var back = VectorMath.AngleToDirection(VectorMath.DirectionToAngle(dir));
            AssertVector(dir, back);
        }
    }

    [Test]
    public void WorldToLocal_ExpresaElPuntoEnElMarcoDelObservador()
    {
        // Observador en (5,5) mirando a la derecha: un punto en (8,6) está 3 delante y 1 a su izquierda.
        var local = VectorMath.WorldToLocal(new Vector2(5f, 5f), Vector2.right, new Vector2(8f, 6f));
        AssertVector(new Vector2(-1f, 3f), local);

        // Mirando arriba, el mismo desplazamiento queda 1 delante y 3 a la derecha.
        local = VectorMath.WorldToLocal(new Vector2(5f, 5f), Vector2.up, new Vector2(8f, 6f));
        AssertVector(new Vector2(3f, 1f), local);
    }

    // ---------- movimiento ----------

    [Test]
    public void StepTowards_AvanzaRapidezPorDeltaTimeEnLaDireccionDelDestino()
    {
        var next = VectorMath.StepTowards(Vector2.zero, new Vector2(10f, 0f), 2f, 0.5f);
        AssertVector(new Vector2(1f, 0f), next);
    }

    [Test]
    public void StepTowards_NoSePasaDelDestino()
    {
        var next = VectorMath.StepTowards(new Vector2(9.9f, 0f), new Vector2(10f, 0f), 2f, 0.5f);
        AssertVector(new Vector2(10f, 0f), next);
    }

    [Test]
    public void SweepAngle_DelGuardiaEstaticoOscilaAlrededorDeLaBase()
    {
        Assert.That(GuardStatic.SweepAngle(90f, 0f, 45f), Is.EqualTo(90f).Within(Eps));
        Assert.That(GuardStatic.SweepAngle(90f, Mathf.PI / 2f, 45f), Is.EqualTo(135f).Within(Eps));
        Assert.That(GuardStatic.SweepAngle(90f, -Mathf.PI / 2f, 45f), Is.EqualTo(45f).Within(Eps));
    }
}
