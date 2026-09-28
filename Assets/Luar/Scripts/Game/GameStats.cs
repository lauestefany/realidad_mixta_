namespace Luar
{
    /// <summary>Datos del recorrido que se muestran en la pantalla de felicitaciones.</summary>
    public static class GameStats
    {
        public static float StartTime;
        public static float Duration;
        public static int Served;
        public static int Mistakes;
        public static bool Completed;

        public static void Reset(float now)
        {
            StartTime = now;
            Duration = 0;
            Served = 0;
            Mistakes = 0;
            Completed = false;
        }
    }
}
