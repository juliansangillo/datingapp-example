namespace API.Helpers {
	public class PaginationParams {
        private const int MAX_PAGE_SIZE = 50;

        private int pageSize = 10;

        public int PageNumber { get; set; } = 1;
        
        public int PageSize {
            get => pageSize;
            set => pageSize = (value > MAX_PAGE_SIZE) ? MAX_PAGE_SIZE : value;
        }
	}
}