namespace Domain.Entity;

public class UserAnimeProgress
{
    
        public Guid Id { get; set; }

        // Внешний ключ на Пользователя
        public Guid UserId { get; set; }
        public User User { get; set; } = null!;

        // Внешний ключ на Аниме в локальной БД
        public Guid AnimeId { get; set; }
        public Anime Anime { get; set; } = null!;

        // Быстрый доступ к MAL ID
        public int MalId { get; set; }

        // Состояние списка и оценки
        public WatchingEnum Status { get; set; } = WatchingEnum.Planned;
        public int EpisodesWatched { get; set; }
        public int? UserScore { get; set; } // 1 - 10
        public bool IsFavorite { get; set; }

        // Прогресс видеоплеера
        public int? LastTranslationId { get; set; }   // ID выбранной озвучки (Kodik)
        public double LastPlaybackPosition { get; set; } // Таймкод остановки (в секундах)

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    
}