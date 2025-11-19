namespace SeguridadSocialApi.Common
{
    /// <summary>
    /// Representa el resultado de una operación que puede tener éxito o fallar
    /// </summary>
    /// <typeparam name="T">Tipo del valor de retorno en caso de éxito</typeparam>
    public class Result<T>
    {
        public bool IsSuccess { get; }
        public T? Value { get; }
        public string? Error { get; }
        public Exception? Exception { get; }

        private Result(bool isSuccess, T? value, string? error, Exception? exception)
        {
            IsSuccess = isSuccess;
            Value = value;
            Error = error;
            Exception = exception;
        }

        /// <summary>
        /// Crea un resultado exitoso
        /// </summary>
        public static Result<T> Success(T value) => new(true, value, null, null);

        /// <summary>
        /// Crea un resultado fallido con mensaje de error
        /// </summary>
        public static Result<T> Failure(string error) => new(false, default, error, null);

        /// <summary>
        /// Crea un resultado fallido con excepción
        /// </summary>
        public static Result<T> Failure(string error, Exception exception) =>
    new(false, default, error, exception);

        /// <summary>
        /// Convierte el resultado a un tipo diferente si fue exitoso
        /// </summary>
        public Result<TNew> Map<TNew>(Func<T, TNew> mapper)
        {
            if (!IsSuccess)
                return Result<TNew>.Failure(Error!, Exception!);

            try
            {
                return Result<TNew>.Success(mapper(Value!));
            }
            catch (Exception ex)
            {
                return Result<TNew>.Failure($"Error al transformar resultado: {ex.Message}", ex);
            }
        }
    }

    /// <summary>
    /// Resultado sin valor de retorno (solo éxito/fallo)
    /// </summary>
    public class Result
    {
        public bool IsSuccess { get; }
        public string? Error { get; }
        public Exception? Exception { get; }

        private Result(bool isSuccess, string? error, Exception? exception)
        {
            IsSuccess = isSuccess;
            Error = error;
            Exception = exception;
        }

        public static Result Success() => new(true, null, null);
        public static Result Failure(string error) => new(false, error, null);
        public static Result Failure(string error, Exception exception) => new(false, error, exception);
    }
}
