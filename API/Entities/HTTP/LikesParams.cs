namespace API.Entities.HTTP {
	public class LikesParams : PaginationParams {
        public int UserId { get; set; }
        public string Predicate { get; set; }
	}
}