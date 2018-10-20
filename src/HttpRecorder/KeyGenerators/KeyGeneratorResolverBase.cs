using System.Collections.Concurrent;

namespace WebApplications.HttpRecorder.KeyGenerators
{
    /// <summary>
    /// Implements base behaviour for the <see cref="IKeyGeneratorResolver"/>, caching
    /// <see cref="IKeyGenerator">key generators</see> automatically.
    /// </summary>
    /// <seealso cref="IKeyGeneratorResolver" />
    public abstract class KeyGeneratorResolverBase : IKeyGeneratorResolver
    {
        /// <summary>
        /// The cache of generators by name
        /// </summary>
        protected readonly ConcurrentDictionary<string, IKeyGenerator> Cache = new ConcurrentDictionary<string, IKeyGenerator>();

        /// <inheritdoc />
        public virtual IKeyGenerator GetKeyGenerator(string name) =>
            string.IsNullOrWhiteSpace(name) ? Default : Cache.GetOrAdd(name, Create);

        /// <inheritdoc />
        public abstract IKeyGenerator Default { get; }

        /// <summary>
        /// Called to create a new <see cref="IKeyGenerator"/> with the <param name="name">specified name</param>.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <returns>The new <see cref="IKeyGenerator"/>.</returns>
        protected abstract IKeyGenerator Create(string name);
    }
}