using System.Configuration;

namespace QuizApplication.Utilities
{
    public class OperationResult
    {
        public bool Success => !Errors.Any();
        public List<string> Errors { get; set; } = new List<string>();

        public static OperationResult Ok() => new OperationResult();
        public static OperationResult Fail(params string[] errors)
        {
            var r = new OperationResult();
            r.Errors.AddRange(errors);
            return r;
        }
    }

    public class OperationResult<T> : OperationResult
    {
        public T? Data { get; set; }

        public static OperationResult<T> Ok(T data) => new OperationResult<T> { Data = data };
        public new static OperationResult<T> Fail(params string[] errors)
        {
            var r = new OperationResult<T>();
            r.Errors.AddRange(errors);
            return r;
        }
    }
}
