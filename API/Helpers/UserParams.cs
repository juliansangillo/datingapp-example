namespace API.Helpers {
	public class UserParams {
        private const int MAX_PAGE_SIZE = 50;

        private int pageSize = 10;

        public int PageNumber { get; set; } = 1;
        
        public int PageSize {
            get => pageSize;
            set => pageSize = (value > MAX_PAGE_SIZE) ? MAX_PAGE_SIZE : value;
        }

        public string CurrentUsername { get; set; }
        public string Gender { get; set; }
        public int MinAge { get; set; } = 18;
        public int MaxAge { get; set; } = 150;
        public string OrderBy { get; set; } = "lastActive";
	}
}