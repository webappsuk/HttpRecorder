using System;

namespace WebApplications.HttpRecorder.KeyGenerators
{
    /// <summary>
    /// The default <see cref="IKeyGeneratorResolver"/> .
    /// </summary>
    /// <seealso cref="KeyGeneratorResolverBase" />
    public class KeyGeneratorResolver : KeyGeneratorResolverBase
    {
        /// <summary>
        /// The builtin prefix character, names starting with this character are reserved for builtin key generators.
        /// </summary>
        public const char BuiltinPrefixChar = '_';

        /// <summary>
        /// The request parts key generator singleton.
        /// </summary>
        public static readonly IKeyGeneratorResolver Instance = new KeyGeneratorResolver();

        /// <summary>
        /// Prevents a default instance of the <see cref="KeyGeneratorResolver"/> class from being created.
        /// </summary>
        private KeyGeneratorResolver()
        {
        }

        /// <summary>
        /// Tries to register a new generator.
        /// </summary>
        /// <param name="generator">The generator.</param>
        /// <returns></returns>
        public bool TryRegister(IKeyGenerator generator)
        {
            if (generator is null)
                return false;

            string name = generator.Name;
            if (string.IsNullOrEmpty(name))
                throw new ArgumentOutOfRangeException(nameof(generator),
                    "The generator name must not be null or empty.");
            if (name[0] == BuiltinPrefixChar)
                throw new ArgumentOutOfRangeException(nameof(generator),
                    $"The generator name '{name}' cannot start with '{BuiltinPrefixChar}' as it is reserved for built-in key generators.");
            return Cache.TryAdd(name, generator);
        }

        /// <summary>
        /// Tries to remove the <see cref="KeyGeneratorBase">generator</see> with the
        /// <paramref name="name">specified name</paramref>.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <param name="generator">The generator.</param>
        /// <returns></returns>
        public bool TryRemove(string name, out IKeyGenerator generator)
        {
            if (string.IsNullOrEmpty(name))
                throw new ArgumentNullException(nameof(name));
            if (name[0] == BuiltinPrefixChar)
                throw new ArgumentOutOfRangeException(nameof(name), name,
                    $"The name '{name}' cannot start with '{BuiltinPrefixChar}' as it is reserved for built-in key generators.");
            return Cache.TryRemove(name, out generator);
        }

        /// <inheritdoc />
        public override IKeyGenerator Default => FullRequestKeyGenerator.Instance;

        /// <inheritdoc />
        protected override IKeyGenerator Create(string name)
        {
            // We can only create built-in key generators, the rest need to be registered
            // using TryRegister above.  By convention built-in will have a three character
            // lead in followed by data.
            if (name.Length < 3 ||
                name[0] != BuiltinPrefixChar)
                throw new ArgumentOutOfRangeException(nameof(name),
                    $"Key generator name '{name}' was not found.");

            string tag = name.Substring(1, 2).ToUpperInvariant();
            switch (tag)
            {
                // Currently we only support request parts, but can expand here
                case RequestPartsKeyGenerator.Prefix:
                    return RequestPartsKeyGenerator.Create(name);
                case FullRequestKeyGenerator.Prefix:
                    return FullRequestKeyGenerator.Instance;
                default:
                    throw new ArgumentOutOfRangeException(nameof(name),
                        $"Key generator name '{name}' is not a valid built-in name.");
            }
        }

        /// <summary>
        /// Gets a key generator that extracts specified <see cref="RequestParts"/>.
        /// </summary>
        /// <param name="parts">The parts.</param>
        /// <returns></returns>
        public virtual IKeyGenerator GetKeyGenerator(RequestParts parts)
            => GetKeyGenerator($"_RP{((ushort)parts).ToString()}");
    }
}