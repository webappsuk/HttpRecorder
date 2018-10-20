namespace WebApplications.HttpRecorder.KeyGenerators
{
    /// <summary>
    /// Interface for supplying keys to a cassette.
    /// </summary>
    public interface IKeyGeneratorResolver
    {
        /// <summary>
        /// Gets the <see cref="KeyGeneratorBase">key generator</see> with the <paramref name="name">specified name</paramref>.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <returns>A <see cref="KeyGeneratorBase">key generator</see></returns>
        IKeyGenerator GetKeyGenerator(string name);

        /// <summary>
        /// Gets the default <see cref="IKeyGenerator"/>.
        /// </summary>
        /// <value>
        /// The default.
        /// </value>
        IKeyGenerator Default { get; }
    }
}