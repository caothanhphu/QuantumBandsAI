namespace QuantumBands.Application.Features.Admin.SystemSettings.Queries
{
    public class GetSystemSettingsQuery
    {
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        public string SortBy { get; set; } = "SettingKey";
        public string SortOrder { get; set; } = "asc";
        public string? SearchTerm { get; set; }
        public bool? IsEditableByAdmin { get; set; }
        public string? DataType { get; set; }

        private const int MaxPageSize = 100;
        public int ValidatedPageSize
        {
            get => (PageSize > MaxPageSize || PageSize <= 0) ? MaxPageSize : PageSize;
            set => PageSize = value;
        }
        public int ValidatedPageNumber
        {
            get => PageNumber <= 0 ? 1 : PageNumber;
            set => PageNumber = value;
        }
    }
}