using System.Globalization;

namespace RandomRec
{
    /// <summary>
    /// Localized strings for the game mode (intro, game-over, hints, play button).
    /// Picks language from the current UI culture, matching App.ApplyLanguage.
    /// </summary>
    internal static class GameText
    {
        private static bool Ru =>
            CultureInfo.CurrentUICulture.TwoLetterISOLanguageName == "ru";

        // ===== Заставка =====
        public static string IntroTitle => Ru ? "НАЙДИ ЗАПИСЬ" : "FIND THE RECORDING";

        public static string IntroDesc => Ru
            ? "Сейчас RandomRec запишет короткий момент и спрячет файл где-то на твоём компьютере. Найди его за отведённое время и перетащи на таймер. По пути будут подсказки."
            : "RandomRec will record a short moment and hide the file somewhere on your computer. Find it within the time limit and drag it onto the timer. Hints will appear along the way.";

        public static string TimeToFind => Ru ? "Время на поиск:" : "Time to find:";
        public static string Seconds => Ru ? "секунд" : "seconds";
        public static string Start => Ru ? "Начать" : "Start";

        // ===== Кнопка на главном окне =====
        public static string Play => Ru ? "Играть…?" : "Play…?";

        // ===== Экран проигрыша =====
        public static string OverTitle => Ru ? "ВРЕМЯ ВЫШЛО" : "TIME'S UP";

        public static string OverDesc => Ru
            ? "Ты не успел найти запись — и она потеряна навсегда. Гусь доволен."
            : "You didn't find the recording in time — and it's gone forever. The goose is pleased.";

        public static string PlayAgain => Ru ? "Играть снова" : "Play again";
        public static string BackToMain => Ru ? "На главный экран" : "Back to main";

        // ===== Лог =====
        public static string LogRecording => Ru
            ? "Игровой режим: идёт запись, потом — прятки…"
            : "Game mode: recording, then hiding…";

        // ===== Таймер и кнопка «Стоп» =====
        public static string Stop => Ru ? "Стоп" : "Stop";

        public static string DropHint => Ru
            ? "Перетащи сюда найденную запись"
            : "Drag the found recording here";

        public static string WrongRecording => Ru
            ? "Это не та запись…"
            : "That's not the recording…";

        // ===== Подсказки =====
        public static string HintDrive(string drive) => Ru
            ? $"Это где-то на диске {drive}"
            : $"It's somewhere on drive {drive}";

        public static string ZoneTemp => Ru
            ? "Загляни туда, где скапливается временный мусор…"
            : "Check where temporary junk piles up…";
        public static string ZoneRoaming => Ru
            ? "Туда, где программы прячут свои настройки…"
            : "Where programs stash their settings…";
        public static string ZoneLocal => Ru
            ? "Где-то в недрах локальных данных приложений…"
            : "Somewhere deep in apps' local data…";
        public static string ZoneDocuments => Ru
            ? "Среди твоих документов…"
            : "Among your documents…";
        public static string ZoneVideos => Ru
            ? "Там, где обычно лежат видео…"
            : "Where videos usually live…";
        public static string ZoneMusic => Ru
            ? "Там, где живёт музыка…"
            : "Where the music lives…";
        public static string ZonePictures => Ru
            ? "Среди картинок…"
            : "Among the pictures…";
        public static string ZoneHome => Ru
            ? "Глубоко в твоей домашней папке…"
            : "Deep in your home folder…";

        public static string HintFolder(string folder) => Ru
            ? $"Ищи в папке с именем «{folder}»"
            : $"Look in a folder named \u201C{folder}\u201D";

        public static string HintExact(string dir) => Ru
            ? $"Точный адрес: {dir}"
            : $"Exact location: {dir}";

        public static string HintFallback => Ru
            ? "Это где-то на твоём компьютере…"
            : "It's somewhere on your computer…";
    }
}