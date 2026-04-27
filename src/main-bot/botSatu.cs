using System;
using System.Drawing;
using Robocode.TankRoyale.BotApi;
using Robocode.TankRoyale.BotApi.Events;

public class TemplateBot : Bot
{
    // ── State musuh ──────────────────────────────────────────────
    private bool  _enemyDetected   = false;
    private double _enemyX         = 0;
    private double _enemyY         = 0;
    private double _enemyVelocity  = 0;
    private double _enemyDirection = 0;   // heading musuh (derajat)
    private int   _lastScanTick    = -1;

    // Jarak aman agar tidak tabrakan (piksel)
    private const double SAFE_DISTANCE    = 10.0;
    // Jarak ideal untuk menyerang
    private const double IDEAL_DISTANCE   = 120.0;
    // Toleransi sudut meriam sebelum tembak (derajat)
    private const double FIRE_ANGLE_TOL   = 3.0;

    static void Main(string[] args) => new TemplateBot().Start();

    TemplateBot() : base(BotInfo.FromFile("botSatu.json")) { }

    // ─────────────────────────────────────────────────────────────
    public override void Run()
    {
        // Radar, Gun, Body bergerak independen
        AdjustRadarForGunTurn  = true;
        AdjustGunForBodyTurn   = true;
        AdjustRadarForBodyTurn = true;   // ← PENTING agar radar tidak ikut body

        BodyColor  = Color.Black;
        GunColor   = Color.Red;
        RadarColor = Color.Yellow;

        while (IsRunning)
        {
            if (!_enemyDetected)
            {
                // ── Mode cari musuh: putar radar 360° terus ──────
                TurnRadarRight(Double.PositiveInfinity); // putar radar satu langkah
            }
            else
            {
                // ── Mode tempur: jalan mendekati musuh sambil radar lock ──
                double dx       = _enemyX - X;
                double dy       = _enemyY - Y;
                double distance = Math.Sqrt(dx * dx + dy * dy);

                // Arah ke musuh
                double angleToEnemy = Math.Atan2(dx, dy) * 180.0 / Math.PI;

                // Putar badan ke arah musuh (agar bisa maju)
                double bodyOffset = NormalizeRelativeAngle(angleToEnemy - Direction);
                TurnRight(bodyOffset);

                // Maju mendekati musuh — BERHENTI jika sudah sangat dekat
                if (distance > IDEAL_DISTANCE)
                {
                    Forward(distance - IDEAL_DISTANCE);
                }
                else if (distance < SAFE_DISTANCE)
                {
                    // Terlalu dekat → mundur hingga aman
                    Back(SAFE_DISTANCE + 5);
                }
                // Jika SAFE_DISTANCE ≤ distance ≤ IDEAL_DISTANCE → diam, fokus tembak

                // Radar tetap putar saat bergerak (jika musuh sudah lama tidak terdeteksi)
                int staleTicks = TurnNumber - _lastScanTick;
                if (staleTicks > 5)
                {
                    // Musuh hilang > 5 tick → kembali ke mode scan
                    _enemyDetected = false;
                }
            }
        }
    }

    // ─────────────────────────────────────────────────────────────
    public override void OnScannedBot(ScannedBotEvent e)
    {
        // ── Simpan data musuh ────────────────────────────────────
        _enemyDetected   = true;
        _enemyX          = e.X;
        _enemyY          = e.Y;
        _enemyVelocity   = e.Speed;
        _enemyDirection  = e.Direction;
        _lastScanTick    = TurnNumber;

        double dx       = e.X - X;
        double dy       = e.Y - Y;
        double distance = Math.Sqrt(dx * dx + dy * dy);

        // ── 1. PREDIKSI POSISI MUSUH ─────────────────────────────
        // Peluru terbang 20 piksel/tick, hitung berapa tick sampai target
        double firePower      = (distance < 100) ? 3.0 : (distance < 200) ? 2.0 : 1.5;
        double bulletSpeed    = 20.0 - 3.0 * firePower;
        double ticksToTarget  = distance / bulletSpeed;

        // Posisi musuh yang diprediksi
        double predX = e.X + Math.Sin(e.Direction * Math.PI / 180.0) * e.Speed * ticksToTarget;
        double predY = e.Y + Math.Cos(e.Direction * Math.PI / 180.0) * e.Speed * ticksToTarget;

        // Sudut ke posisi prediksi
        double pdx            = predX - X;
        double pdy            = predY - Y;
        double predictedAngle = Math.Atan2(pdx, pdy) * 180.0 / Math.PI;

        // ── 2. LOCK RADAR ─────────────────────────────────────────
        double radarOffset = NormalizeRelativeAngle(predictedAngle - RadarDirection);
        // Overkill sedikit agar radar tidak kehilangan target
        TurnRadarRight(radarOffset * 1.9);

        // ── 3. LOCK MERIAM ke posisi PREDIKSI ────────────────────
        double gunOffset = NormalizeRelativeAngle(predictedAngle - GunDirection);
        TurnGunRight(gunOffset);

        // ── 4. LOGIKA BERHENTI (SENSOR JARAK) ────────────────────
        // Jika sudah terlalu dekat, jangan maju lagi — cukup tembak
        if (distance <= SAFE_DISTANCE)
        {
            // Tidak bergerak maju; hanya kunci senjata & tembak
            SetBack(0);   // pastikan tidak ada perintah gerak aktif
        }

        // ── 5. TEMBAK jika meriam sudah cukup akurat ─────────────
        if (Math.Abs(gunOffset) < FIRE_ANGLE_TOL && GunHeat == 0)
        {
            Fire(firePower);
        }
    }

    // ─────────────────────────────────────────────────────────────
    public override void OnHitWall(HitWallEvent e)
    {
        // Mundur dan belok 90° jika nabrak tembok
        Back(30);
        TurnRight(90);

        // Reset deteksi agar radar kembali scan
        _enemyDetected = false;
    }

    public override void OnHitBot(HitBotEvent e)
    {
        // Nabrak robot lain → mundur segera
        Back(SAFE_DISTANCE + 10);
    }
}