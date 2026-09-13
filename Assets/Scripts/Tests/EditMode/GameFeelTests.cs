using NUnit.Framework;
using UnityEngine;

public class GameFeelTests
{
    const float Eps = 1e-4f;

    [Test]
    public void Bezier_EmpiezaEnP0YTerminaEnP2()
    {
        var p0 = new Vector3(0f, 0f, 0f);
        var p1 = new Vector3(1f, 3f, 0f);
        var p2 = new Vector3(2f, 0f, 0f);
        Assert.That(Vector3.Distance(FlyingSprite.Bezier(p0, p1, p2, 0f), p0), Is.LessThan(Eps));
        Assert.That(Vector3.Distance(FlyingSprite.Bezier(p0, p1, p2, 1f), p2), Is.LessThan(Eps));
    }

    [Test]
    public void Bezier_EnElMedioSeElevaHaciaElPuntoDeControl()
    {
        var p0 = new Vector3(0f, 0f, 0f);
        var p1 = new Vector3(1f, 3f, 0f);
        var p2 = new Vector3(2f, 0f, 0f);
        var mid = FlyingSprite.Bezier(p0, p1, p2, 0.5f);
        Assert.That(mid.x, Is.EqualTo(1f).Within(Eps));
        Assert.That(mid.y, Is.EqualTo(1.5f).Within(Eps));   // la mitad de la altura del control
    }

    [Test]
    public void ControlPoint_EstaSobreElPuntoMedioYSubeConLaDistancia()
    {
        var a = FlyingSprite.ControlPoint(Vector3.zero, new Vector3(4f, 0f, 0f), 0.5f);
        var b = FlyingSprite.ControlPoint(Vector3.zero, new Vector3(8f, 0f, 0f), 0.5f);
        Assert.That(a.x, Is.EqualTo(2f).Within(Eps));
        Assert.That(b.x, Is.EqualTo(4f).Within(Eps));
        Assert.That(b.y, Is.GreaterThan(a.y));
    }

    [Test]
    public void CaptureSubtitle_DistingueSinVidasUltimaVidaYVariasVidas()
    {
        Assert.That(GameManager.CaptureSubtitle(0), Does.Contain("SIN VIDAS"));
        Assert.That(GameManager.CaptureSubtitle(1), Does.Contain("ÚLTIMA VIDA"));
        Assert.That(GameManager.CaptureSubtitle(2), Does.Contain("2 VIDAS"));
    }

    [Test]
    public void ExitSubtitle_IncluyeElTiempoYSoloAnunciaRecordCuandoLoEs()
    {
        Assert.That(GameManager.ExitSubtitle(65.5f, false), Is.EqualTo("TIEMPO 01:05.50"));
        Assert.That(GameManager.ExitSubtitle(65.5f, true), Does.Contain("RÉCORD"));
    }

    [Test]
    public void StatsFor_VictoriaYDerrotaMuestranLoQueImporta()
    {
        string win = GameOverUI.StatsFor(true, 65.5f, true, 60f, 3, 5, 5);
        Assert.That(win, Does.Contain("01:05.50").And.Contain("RÉCORD").And.Contain("CAPTURAS  3"));
        string lose = GameOverUI.StatsFor(false, 0f, false, 0f, 7, 2, 5);
        Assert.That(lose, Does.Contain("CAPTURAS  7").And.Contain("2/5"));
    }

    [Test]
    public void RoomHint_LaPrimeraSalaEnsenaLosControles()
    {
        Assert.That(GameManager.RoomHint(1), Does.Contain("WASD"));
        Assert.That(GameManager.RoomHint(2), Does.Contain("LLAVE"));
        Assert.That(GameManager.RoomHint(3), Does.Contain("PIEDRA"));
        Assert.That(GameManager.RoomHint(5), Is.Not.Empty);
    }
}
