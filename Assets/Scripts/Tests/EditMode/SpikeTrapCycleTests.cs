using NUnit.Framework;
using UnityEngine;

public class SpikeTrapCycleTests
{
    const float Hidden = 1.6f, Rising = 0.25f, Extended = 0.9f, Falling = 0.25f;

    static SpikeTrap.Phase At(float t) => SpikeTrap.PhaseAt(t, Hidden, Rising, Extended, Falling);

    [Test]
    public void ArrancaOculta()
    {
        Assert.AreEqual(SpikeTrap.Phase.Hidden, At(0f));
        Assert.AreEqual(SpikeTrap.Phase.Hidden, At(1.5f));
    }

    [Test]
    public void RecorreLasCuatroFasesEnOrden()
    {
        Assert.AreEqual(SpikeTrap.Phase.Rising, At(1.7f));
        Assert.AreEqual(SpikeTrap.Phase.Extended, At(2.0f));
        Assert.AreEqual(SpikeTrap.Phase.Falling, At(2.8f));
    }

    [Test]
    public void ElCicloSeRepite()
    {
        float period = Hidden + Rising + Extended + Falling;
        // Se muestrea en el CENTRO de cada fase para no depender de la precisión
        // en coma flotante justo en las fronteras del ciclo.
        float[] centros = { Hidden / 2f, Hidden + Rising / 2f,
                            Hidden + Rising + Extended / 2f,
                            Hidden + Rising + Extended + Falling / 2f };
        foreach (float t in centros)
        {
            Assert.AreEqual(At(t), At(t + period), $"desfase de un ciclo en t={t}");
            Assert.AreEqual(At(t), At(t + period * 3f), $"desfase de tres ciclos en t={t}");
        }
    }

    [Test]
    public void SoloEsLetalEnLaFaseExtendida()
    {
        int deadly = 0, total = 0;
        float period = Hidden + Rising + Extended + Falling;
        for (float t = 0f; t < period; t += 0.01f)
        {
            total++;
            if (At(t) == SpikeTrap.Phase.Extended) deadly++;
        }
        float ratio = (float)deadly / total;
        // La ventana peligrosa debe ser minoritaria: la sala tiene que poder cruzarse.
        Assert.Less(ratio, 0.5f);
        Assert.Greater(ratio, 0.1f);
    }

    [TestCase(2f)]   // dos trampas a medio ciclo
    [TestCase(3f)]   // tres trampas a un tercio de ciclo: lo que coloca Semana04Builder
    public void ElDesfaseSeparaLasTrampasVecinas(float divisiones)
    {
        float period = Hidden + Rising + Extended + Falling;
        float desfase = period / divisiones;
        // La ventana letal (0.9 s) debe caber en el hueco entre trampas consecutivas.
        Assert.Less(Extended, desfase, "la ventana letal no cabe entre trampas");

        for (float t = 0f; t < period; t += 0.01f)
        {
            bool a = At(t) == SpikeTrap.Phase.Extended;
            bool b = At(t + desfase) == SpikeTrap.Phase.Extended;
            Assert.IsFalse(a && b, $"ambas letales en t={t} con desfase {desfase}");
        }
    }

    [Test]
    public void PeriodoDegeneradoNoDivideEntreCero()
    {
        Assert.DoesNotThrow(() => SpikeTrap.PhaseAt(3f, 0f, 0f, 0f, 0f));
    }
}
