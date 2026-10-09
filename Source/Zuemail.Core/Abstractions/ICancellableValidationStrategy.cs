using System.Threading;
using System.Threading.Tasks;

namespace Zuemail.Core.Abstractions
{
    /// <summary>Optional cancellation support without breaking existing validation strategies.</summary>
    /// <typeparam name="T">The item validated.</typeparam>
    public interface ICancellableValidationStrategy<T> : IValidationStrategy<T>
    {
        /// <summary>Validates an item while observing cancellation.</summary>
        /// <param name="item">The item validated.</param>
        /// <param name="cancellationToken">Cancels asynchronous validation.</param>
        /// <returns>Whether the item is valid.</returns>
        Task<bool> ValidateAsync(T item, CancellationToken cancellationToken);
    }
}