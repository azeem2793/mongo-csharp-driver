/* Copyright 2010-present MongoDB Inc.
*
* Licensed under the Apache License, Version 2.0 (the "License");
* you may not use this file except in compliance with the License.
* You may obtain a copy of the License at
*
* http://www.apache.org/licenses/LICENSE-2.0
*
* Unless required by applicable law or agreed to in writing, software
* distributed under the License is distributed on an "AS IS" BASIS,
* WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
* See the License for the specific language governing permissions and
* limitations under the License.
*/

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using MongoDB.Bson;
using MongoDB.Driver.Core.Clusters;
using MongoDB.Driver.Core.Configuration;
using MongoDB.Driver.Core.Misc;
using MongoDB.Driver.Encryption;
using MongoDB.Driver.Linq;
using MongoDB.Shared;

namespace MongoDB.Driver
{
    /// <summary>
    /// The settings for a MongoDB client.
    /// </summary>
    public class MongoClientSettings : IEquatable<MongoClientSettings>, IInheritableMongoClientSettings
    {
        // private fields
        private bool _allowInsecureTls;
        private string _applicationName;
        private AutoEncryptionOptions _autoEncryptionOptions;
        private Action<ClusterBuilder> _clusterConfigurator;
        private IReadOnlyList<CompressorConfiguration> _compressors;
#pragma warning disable CS0618 // Type or member is obsolete
        private ConnectionMode _connectionMode;
        private ConnectionModeSwitch _connectionModeSwitch;
#pragma warning restore CS0618 // Type or member is obsolete
        private TimeSpan _connectTimeout;
        private MongoCredentialStore _credentials;
        private bool? _directConnection;
        private GuidRepresentation _guidRepresentation;
        private TimeSpan _heartbeatInterval;
        private TimeSpan _heartbeatTimeout;
        private bool _ipv6;
        private LinqProvider _linqProvider;
        private bool _loadBalanced;
        private TimeSpan _localThreshold;
        private LoggingSettings _loggingSettings;
        private int _maxConnecting;
        private TimeSpan _maxConnectionIdleTime;
        private TimeSpan _maxConnectionLifeTime;
        private int _maxConnectionPoolSize;
        private int _minConnectionPoolSize;
        private ReadConcern _readConcern;
        private UTF8Encoding _readEncoding;
        private ReadPreference _readPreference;
        private string _replicaSetName;
        private bool _retryReads;
        private bool _retryWrites;
        private ConnectionStringScheme _scheme;
        private string _sdamLogFilename;
        private ServerApi _serverApi;
        private List<MongoServerAddress> _servers;
        private TimeSpan _serverSelectionTimeout;
        private TimeSpan _socketTimeout;
        private int _srvMaxHosts;
        private SslSettings _sslSettings;
        private bool _useTls;
        private int _waitQueueSize;
        private TimeSpan _waitQueueTimeout;
        private WriteConcern _writeConcern;
        private UTF8Encoding _writeEncoding;

        // the following fields are set when Freeze is called
        private bool _isFrozen;
        private int _frozenHashCode;
        private string _frozenStringRepresentation;

        // constructors
        /// <summary>
        /// Creates a new instance of MongoClientSettings. Usually you would use a connection string instead.
        /// </summary>
        public MongoClientSettings()
        {
            _allowInsecureTls = false;
            _applicationName = null;
            _autoEncryptionOptions = null;
            _compressors = new CompressorConfiguration[0];
#pragma warning disable CS0618 // Type or member is obsolete
            _connectionMode = ConnectionMode.Automatic;
            _connectionModeSwitch = ConnectionModeSwitch.NotSet;
#pragma warning restore CS0618 // Type or member is obsolete
            _connectTimeout = MongoDefaults.ConnectTimeout;
            _credentials = new MongoCredentialStore(new MongoCredential[0]);
            _directConnection = null;
#pragma warning disable 618
            if (BsonDefaults.GuidRepresentationMode == GuidRepresentationMode.V2)
            {
                _guidRepresentation = MongoDefaults.GuidRepresentation;
            }
#pragma warning restore 618
            _heartbeatInterval = ServerSettings.DefaultHeartbeatInterval;
            _heartbeatTimeout = ServerSettings.DefaultHeartbeatTimeout;
            _ipv6 = false;
            _linqProvider = LinqProvider.V3;
            _loadBalanced = false;
            _localThreshold = MongoDefaults.LocalThreshold;
            _maxConnecting = MongoInternalDefaults.ConnectionPool.MaxConnecting;
            _maxConnectionIdleTime = MongoDefaults.MaxConnectionIdleTime;
            _maxConnectionLifeTime = MongoDefaults.MaxConnectionLifeTime;
            _maxConnectionPoolSize = MongoDefaults.MaxConnectionPoolSize;
            _minConnectionPoolSize = MongoDefaults.MinConnectionPoolSize;
            _readConcern = ReadConcern.Default;
            _readEncoding = null;
            _readPreference = ReadPreference.Primary;
            _replicaSetName = null;
            _retryReads = true;
            _retryWrites = true;
            _scheme = ConnectionStringScheme.MongoDB;
            _sdamLogFilename = null;
            _serverApi = null;
            _servers = new List<MongoServerAddress> { new MongoServerAddress("localhost") };
            _serverSelectionTimeout = MongoDefaults.ServerSelectionTimeout;
            _socketTimeout = MongoDefaults.SocketTimeout;
            _srvMaxHosts = 0;
            _sslSettings = null;
            _useTls = false;
#pragma warning disable 618
            _waitQueueSize = MongoDefaults.ComputedWaitQueueSize;
#pragma warning restore 618
            _waitQueueTimeout = MongoDefaults.WaitQueueTimeout;
            _writeConcern = WriteConcern.Acknowledged;
            _writeEncoding = null;
        }

        // public properties
        /// <summary>
        /// Gets or sets whether to relax TLS constraints as much as possible.
        /// Setting this variable to true will also set SslSettings.CheckCertificateRevocation to false.
        /// </summary>
        public bool AllowInsecureTls
        {
            get { return _allowInsecureTls; }
            set
            {
                if (_isFrozen) { throw new InvalidOperationException("MongoClientSettings is frozen."); }
                if (value)
                {
                    _sslSettings = _sslSettings ?? new SslSettings();
                    // Otherwise, the user will have to manually set CheckCertificateRevocation to false
                    _sslSettings.CheckCertificateRevocation = false;
                }
                _allowInsecureTls = value;
            }
        }

        /// <summary>
        /// Gets or sets the application name.
        /// </summary>
        public string ApplicationName
        {
            get { return _applicationName; }
            set
            {
                if (_isFrozen) { throw new InvalidOperationException("MongoClientSettings is frozen."); }
                _applicationName = ApplicationNameHelper.EnsureApplicationNameIsValid(value, nameof(value));
            }
        }

        /// <summary>
        /// Gets or sets the auto encryption options.
        /// </summary>
        public AutoEncryptionOptions AutoEncryptionOptions
        {
            get { return _autoEncryptionOptions; }
            set
            {
                if (_isFrozen) { throw new InvalidOperationException("MongoClientSettings is frozen."); }
                _autoEncryptionOptions = value;
            }
        }

        /// <summary>
        /// Gets or sets the compressors.
        /// </summary>
        public IReadOnlyList<CompressorConfiguration> Compressors
        {
            get { return _compressors; }
            set
            {
                if (_isFrozen) { throw new InvalidOperationException("MongoClientSettings is frozen."); }
                _compressors = value;
            }
        }

        /// <summary>
        /// Gets or sets the cluster configurator.
        /// </summary>
        public Action<ClusterBuilder> ClusterConfigurator
        {
            get { return _clusterConfigurator; }
            set
            {
                if (_isFrozen) { throw new InvalidOperationException("MongoClientSettings is frozen."); }
                _clusterConfigurator = value;
            }
        }

        /// <summary>
        /// Gets or sets the connection mode.
        /// </summary>
        [Obsolete("Use DirectConnection instead.")]
        public ConnectionMode ConnectionMode
        {
            get
            {
                if (_connectionModeSwitch == ConnectionModeSwitch.UseDirectConnection)
                {
                    throw new InvalidOperationException("ConnectionMode cannot be used when ConnectionModeSwitch is set to UseDirectConnection.");
                }

                return _connectionMode;
            }
            set
            {
                if (_isFrozen) { throw new InvalidOperationException("MongoClientSettings is frozen."); }
                if (_connectionModeSwitch == ConnectionModeSwitch.UseDirectConnection)
                {
                    throw new InvalidOperationException("ConnectionMode cannot be used when ConnectionModeSwitch is set to UseDirectConnection.");
                }

                _connectionMode = value;
                _connectionModeSwitch = ConnectionModeSwitch.UseConnectionMode; // _directConnection is always null here
            }
        }

        /// <summary>
        /// Gets the connection mode switch.
        /// </summary>
        [Obsolete("This property will be removed in a later release.")]
        public ConnectionModeSwitch ConnectionModeSwitch
        {
            get
            {
                return _connectionModeSwitch;
            }
        }

        /// <summary>
        /// Gets or sets the connect timeout.
        /// </summary>
        public TimeSpan ConnectTimeout
        {
            get { return _connectTimeout; }
            set
            {
                if (_isFrozen) { throw new InvalidOperationException("MongoClientSettings is frozen."); }
                _connectTimeout = value;
            }
        }

        /// <summary>
        /// Gets or sets the credential.
        /// </summary>
        public MongoCredential Credential
        {
            get
            {
                return _credentials.SingleOrDefault();
            }
            set
            {
                if (_isFrozen) { throw new InvalidOperationException("MongoClientSettings is frozen."); }
                if (value == null)
                {
                    _credentials = new MongoCredentialStore(Enumerable.Empty<MongoCredential>());
                }
                else
                {
                    _credentials = new MongoCredentialStore(new[] { value });
                }
            }
        }
        /// <summary>
        /// Gets or sets the credentials.
        /// </summary>
        [Obsolete("Use Credential instead. Using multiple credentials is deprecated.")]
        public IEnumerable<MongoCredential> Credentials
        {
            get { return _credentials; }
            set
            {
                if (_isFrozen) { throw new InvalidOperationException("MongoClientSettings is frozen."); }
                if (value == null)
                {
                    throw new ArgumentNullException("value");
                }
                _credentials = new MongoCredentialStore(value);
            }
        }

        /// <summary>
        /// Gets or sets the direct connection.
        /// </summary>
        public bool? DirectConnection
        {
            get
            {
#pragma warning disable CS0618 // Type or member is obsolete
                if (_connectionModeSwitch == ConnectionModeSwitch.UseConnectionMode)
#pragma warning restore CS0618 // Type or member is obsolete
                {
                    throw new InvalidOperationException("DirectConnection cannot be used when ConnectionModeSwitch is set to UseConnectionMode.");
                }

                return _directConnection;
            }
            set
            {
                if (_isFrozen) { throw new InvalidOperationException("MongoClientSettings is frozen."); }
#pragma warning disable CS0618 // Type or member is obsolete
                if (_connectionModeSwitch == ConnectionModeSwitch.UseConnectionMode)
                {
                    throw new InvalidOperationException("DirectConnection cannot be used when ConnectionModeSwitch is set to UseConnectionMode.");
                }

                _directConnection = value;
                _connectionModeSwitch = ConnectionModeSwitch.UseDirectConnection; // _connectionMode is always Automatic here
#pragma warning restore CS0618 // Type or member is obsolete
            }
        }

        /// <summary>
        /// Gets or sets the representation to use for Guids.
        /// </summary>
        [Obsolete("Configure serializers instead.")]
        public GuidRepresentation GuidRepresentation
        {
            get
            {
                if (BsonDefaults.GuidRepresentationMode != GuidRepresentationMode.V2)
                {
                    throw new InvalidOperationException("MongoClientSettings.GuidRepresentation can only be used when BsonDefaults.GuidRepresentationModes is V2.");
                }
                return _guidRepresentation;
            }
            set
            {
                if (_isFrozen) { throw new InvalidOperationException("MongoClientSettings is frozen."); }
                if (BsonDefaults.GuidRepresentationMode != GuidRepresentationMode.V2)
                {
                    throw new InvalidOperationException("MongoClientSettings.GuidRepresentation can only be used when BsonDefaults.GuidRepresentationModes is V2.");
                }
                _guidRepresentation = value;
            }
        }

        /// <summary>
        /// Gets a value indicating whether the settings have been frozen to prevent further changes.
        /// </summary>
        public bool IsFrozen
        {
            get { return _isFrozen; }
        }

        /// <summary>
        /// Gets or sets the heartbeat interval.
        /// </summary>
        public TimeSpan HeartbeatInterval
        {
            get { return _heartbeatInterval; }
            set
            {
                if (_isFrozen) { throw new InvalidOperationException("MongoClientSettings is frozen."); }
                _heartbeatInterval = Ensure.IsGreaterThanZero(value, nameof(value));
            }
        }

        /// <summary>
        /// Gets or sets the heartbeat timeout.
        /// </summary>
        public TimeSpan HeartbeatTimeout
        {
            get { return _heartbeatTimeout; }
            set
            {
                if (_isFrozen) { throw new InvalidOperationException("MongoClientSettings is frozen."); }
                _heartbeatTimeout = Ensure.IsInfiniteOrGreaterThanZero(value, nameof(value));
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether to use IPv6.
        /// </summary>
        public bool IPv6
        {
            get { return _ipv6; }
            set
            {
                if (_isFrozen) { throw new InvalidOperationException("MongoClientSettings is frozen."); }
                _ipv6 = value;
            }
        }

        /// <summary>
        /// Gets or sets the LINQ provider.
        /// </summary>
        public LinqProvider LinqProvider
        {
            get { return _linqProvider; }
            set
            {
                if (_isFrozen) { throw new InvalidOperationException("MongoClientSettings is frozen."); }
                _linqProvider = value;
            }
        }

        /// <summary>
        /// Gets or sets whether load balanced mode is used.
        /// </summary>
        public bool LoadBalanced
        {
            get { return _loadBalanced; }
            set
            {
                if (_isFrozen) { throw new InvalidOperationException("MongoClientSettings is frozen."); }
                _loadBalanced = value;
            }
        }

        /// <summary>
        /// Gets or sets the local threshold.
        /// </summary>
        public TimeSpan LocalThreshold
        {
            get { return _localThreshold; }
            set
            {
                if (_isFrozen) { throw new InvalidOperationException("MongoClientSettings is frozen."); }
                _localThreshold = value;
            }
        }

        /// <summary>
        /// Gets or sets the logging settings
        /// </summary>
        public LoggingSettings LoggingSettings
        {
            get { return _loggingSettings; }
            set
            {
                if (_isFrozen) { throw new InvalidOperationException("MongoClientSettings is frozen."); }
                _loggingSettings = value;
            }
        }

        /// <summary>
        /// Gets or sets the maximum concurrently connecting connections.
        /// </summary>
        public int MaxConnecting
        {
            get { return _maxConnecting; }
            set
            {
                ThrowIfFrozen();
                _maxConnecting = Ensure.IsGreaterThanZero(value, nameof(MaxConnecting));
            }
        }

        /// <summary>
        /// Gets or sets the max connection idle time.
        /// </summary>
        public TimeSpan MaxConnectionIdleTime
        {
            get { return _maxConnectionIdleTime; }
            set
            {
                if (_isFrozen) { throw new InvalidOperationException("MongoClientSettings is frozen."); }
                _maxConnectionIdleTime = value;
            }
        }

        /// <summary>
        /// Gets or sets the max connection life time.
        /// </summary>
        public TimeSpan MaxConnectionLifeTime
        {
            get { return _maxConnectionLifeTime; }
            set
            {
                if (_isFrozen) { throw new InvalidOperationException("MongoClientSettings is frozen."); }
                _maxConnectionLifeTime = value;
            }
        }

        /// <summary>
        /// Gets or sets the max connection pool size.
        /// </summary>
        public int MaxConnectionPoolSize
        {
            get { return _maxConnectionPoolSize; }
            set
            {
                if (_isFrozen) { throw new InvalidOperationException("MongoClientSettings is frozen."); }
                _maxConnectionPoolSize = Ensure.IsGreaterThanZero(value, nameof(MaxConnectionPoolSize));
            }
        }

        /// <summary>
        /// Gets or sets the min connection pool size.
        /// </summary>
        public int MinConnectionPoolSize
        {
            get { return _minConnectionPoolSize; }
            set
            {
                if (_isFrozen) { throw new InvalidOperationException("MongoClientSettings is frozen."); }
                _minConnectionPoolSize = value;
            }
        }

        /// <summary>
        /// Gets or sets the read concern.
        /// </summary>
        public ReadConcern ReadConcern
        {
            get { return _readConcern; }
            set
            {
                if (_isFrozen) { throw new InvalidOperationException("MongoClientSettings is frozen."); }
                _readConcern = Ensure.IsNotNull(value, nameof(value));
            }
        }

        /// <summary>
        /// Gets or sets the Read Encoding.
        /// </summary>
        public UTF8Encoding ReadEncoding
        {
            get { return _readEncoding; }
            set
            {
                if (_isFrozen) { throw new InvalidOperationException("MongoClientSettings is frozen."); }
                _readEncoding = value;
            }
        }

        /// <summary>
        /// Gets or sets the read preferences.
        /// </summary>
        public ReadPreference ReadPreference
        {
            get { return _readPreference; }
            set
            {
                if (_isFrozen) { throw new InvalidOperationException("MongoClientSettings is frozen."); }
                if (value == null)
                {
                    throw new ArgumentNullException("value");
                }
                _readPreference = value;
            }
        }

        /// <summary>
        /// Gets or sets the name of the replica set.
        /// </summary>
        public string ReplicaSetName
        {
            get { return _replicaSetName; }
            set
            {
                if (_isFrozen) { throw new InvalidOperationException("MongoClientSettings is frozen."); }
                _replicaSetName = value;
            }
        }

        /// <summary>
        /// Gets or sets whether to retry reads.
        /// </summary>
        public bool RetryReads
        {
            get { return _retryReads; }
            set
            {
                if (_isFrozen) { throw new InvalidOperationException("MongoClientSettings is frozen."); }
                _retryReads = value;
            }
        }

        /// <summary>
        /// Gets or sets whether to retry writes.
        /// </summary>
        /// <value>
        /// The default value is <c>true</c>.
        /// </value>
        public bool RetryWrites
        {
            get { return _retryWrites; }
            set
            {
                if (_isFrozen) { throw new InvalidOperationException("MongoClientSettings is frozen."); }
                _retryWrites = value;
            }
        }

        /// <summary>
        /// Gets or sets the connection string scheme.
        /// </summary>
        public ConnectionStringScheme Scheme
        {
            get { return _scheme; }
            set
            {
                if (_isFrozen) { throw new InvalidOperationException("MongoClientSettings is frozen."); }
                _scheme = value;
            }
        }

        /// <summary>
        /// Gets or set the name of the SDAM log file. Null turns logging off. stdout will log to console.
        /// </summary>
        [Obsolete("Use LoggerFactory instead.")]
        public string SdamLogFilename
        {
            get { return _sdamLogFilename; }
            set
            {
                if (_isFrozen) { throw new InvalidOperationException("MongoClientSettings is frozen."); }
                _sdamLogFilename = value;
            }
        }

        /// <summary>
        /// Gets or sets the server API.
        /// </summary>
        public ServerApi ServerApi
        {
            get { return _serverApi; }
            set
            {
                if (_isFrozen) { throw new InvalidOperationException("MongoClientSettings is frozen."); }
                _serverApi = value;
            }
        }

        /// <summary>
        /// Gets or sets the address of the server (see also Servers if using more than one address).
        /// </summary>
        public MongoServerAddress Server
        {
            get { return _servers.Single(); }
            set
            {
                if (_isFrozen) { throw new InvalidOperationException("MongoClientSettings is frozen."); }
                if (value == null)
                {
                    throw new ArgumentNullException(nameof(value));
                }
                _servers = new List<MongoServerAddress> { value };
            }
        }

        /// <summary>
        /// Gets or sets the list of server addresses (see also Server if using only one address).
        /// </summary>
        public IEnumerable<MongoServerAddress> Servers
        {
            get
            {
                var servers = _srvMaxHosts > 0 ? _servers.Take(_srvMaxHosts).ToList() : _servers;
                return new ReadOnlyCollection<MongoServerAddress>(servers);
            }
            set
            {
                if (_isFrozen) { throw new InvalidOperationException("MongoClientSettings is frozen."); }
                if (value == null)
                {
                    throw new ArgumentNullException(nameof(value));
                }

                var servers = new List<MongoServerAddress>(value);
                if (_srvMaxHosts > 0)
                {
                    FisherYatesShuffle.Shuffle(servers);
                }

                _servers = servers;
            }
        }

        /// <summary>
        /// Gets or sets the server selection timeout.
        /// </summary>
        public TimeSpan ServerSelectionTimeout
        {
            get { return _serverSelectionTimeout; }
            set
            {
                if (_isFrozen) { throw new InvalidOperationException("MongoClientSettings is frozen."); }
                _serverSelectionTimeout = value;
            }
        }

        /// <summary>
        /// Gets or sets the socket timeout.
        /// </summary>
        public TimeSpan SocketTimeout
        {
            get { return _socketTimeout; }
            set
            {
                if (_isFrozen) { throw new InvalidOperationException("MongoClientSettings is frozen."); }
                _socketTimeout = value;
            }
        }

        /// <summary>
        /// Limits the number of SRV records used to populate the seedlist
        /// during initial discovery, as well as the number of additional hosts
        /// that may be added during SRV polling.
        /// </summary>
        public int SrvMaxHosts
        {
            get { return _srvMaxHosts; }
            set
            {
                if (_isFrozen) { throw new InvalidOperationException("MongoClientSettings is frozen."); }
                _srvMaxHosts = Ensure.IsGreaterThanOrEqualToZero(value, nameof(value));
            }
        }

        /// <summary>
        /// Gets or sets the SSL settings.
        /// </summary>
        public SslSettings SslSettings
        {
            get { return _sslSettings; }
            set
            {
                if (_isFrozen) { throw new InvalidOperationException("MongoClientSettings is frozen."); }
                _sslSettings = value;
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether to use SSL.
        /// </summary>
        [Obsolete("Use UseTls instead.")]
        public bool UseSsl
        {
            get { return _useTls; }
            set
            {
                if (_isFrozen) { throw new InvalidOperationException("MongoClientSettings is frozen."); }
                _useTls = value;
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether to use TLS.
        /// </summary>
        public bool UseTls
        {
            get { return _useTls; }
            set
            {
                if (_isFrozen) { throw new InvalidOperationException("MongoClientSettings is frozen."); }
                _useTls = value;
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether to verify an SSL certificate.
        /// </summary>
        [Obsolete("Use AllowInsecureTls instead.")]
        public bool VerifySslCertificate
        {
            get { return !_allowInsecureTls; }
            set
            {
                if (_isFrozen) { throw new InvalidOperationException("MongoClientSettings is frozen."); }
                // use property instead of private field because setter has additional side effects
                AllowInsecureTls = !value;
            }
        }

        /// <summary>
        /// Gets or sets the wait queue size.
        /// </summary>
        [Obsolete("This property will be removed in a later release.")]
        public int WaitQueueSize
        {
            get { return _waitQueueSize; }
            set
            {
                if (_isFrozen) { throw new InvalidOperationException("MongoClientSettings is frozen."); }
                _waitQueueSize = value;
            }
        }

        /// <summary>
        /// Gets or sets the wait queue timeout.
        /// </summary>
        public TimeSpan WaitQueueTimeout
        {
            get { return _waitQueueTimeout; }
            set
            {
                if (_isFrozen) { throw new InvalidOperationException("MongoClientSettings is frozen."); }
                _waitQueueTimeout = value;
            }
        }

        /// <summary>
        /// Gets or sets the WriteConcern to use.
        /// </summary>
        public WriteConcern WriteConcern
        {
            get { return _writeConcern; }
            set
            {
                if (_isFrozen) { throw new InvalidOperationException("MongoClientSettings is frozen."); }
                if (value == null)
                {
                    throw new ArgumentNullException("value");
                }
                _writeConcern = value;
            }
        }

        /// <summary>
        /// Gets or sets the Write Encoding.
        /// </summary>
        public UTF8Encoding WriteEncoding
        {
            get { return _writeEncoding; }
            set
            {
                if (_isFrozen) { throw new InvalidOperationException("MongoClientSettings is frozen."); }
                _writeEncoding = value;
            }
        }

        // public operators
        /// <summary>
        /// Determines whether two <see cref="MongoClientSettings"/> instances are equal.
        /// </summary>
        /// <param name="lhs">The LHS.</param>
        /// <param name="rhs">The RHS.</param>
        /// <returns>
        ///   <c>true</c> if the left hand side is equal to the right hand side; otherwise, <c>false</c>.
        /// </returns>
        public static bool operator ==(MongoClientSettings lhs, MongoClientSettings rhs)
        {
            return object.Equals(lhs, rhs); // handles lhs == null correctly
        }

        /// <summary>
        /// Determines whether two <see cref="MongoClientSettings"/> instances are not equal.
        /// </summary>
        /// <param name="lhs">The LHS.</param>
        /// <param name="rhs">The RHS.</param>
        /// <returns>
        ///   <c>true</c> if the left hand side is not equal to the right hand side; otherwise, <c>false</c>.
        /// </returns>
        public static bool operator !=(MongoClientSettings lhs, MongoClientSettings rhs)
        {
            return !(lhs == rhs);
        }

        // public static methods
        /// <summary>
        /// Gets a MongoClientSettings object intialized with values from a connection string.
        /// </summary>
        /// <param name="connectionString">The connection string.</param>
        /// <returns>A MongoClientSettings.</returns>
        public static MongoClientSettings FromConnectionString(string connectionString)
        {
            return FromUrl(new MongoUrl(connectionString));
        }

        /// <summary>
        /// Gets a MongoClientSettings object intialized with values from a MongoURL.
        /// </summary>
        /// <param name="url">The MongoURL.</param>
        /// <returns>A MongoClientSettings.</returns>
        public static MongoClientSettings FromUrl(MongoUrl url)
        {
            if (!url.IsResolved)
            {
                bool resolveHosts;
#pragma warning disable CS0618 // Type or member is obsolete
                if (url.ConnectionModeSwitch == ConnectionModeSwitch.UseDirectConnection)
#pragma warning restore CS0618 // Type or member is obsolete
                {
                    resolveHosts = url.DirectConnection.GetValueOrDefault();
                }
                else
                {
#pragma warning disable CS0618 // Type or member is obsolete
                    var connectionMode = url.ConnectionMode;
#pragma warning restore CS0618 // Type or member is obsolete
                    resolveHosts = connectionMode == ConnectionMode.Direct || connectionMode == ConnectionMode.Standalone;
                }
                url = url.Resolve(resolveHosts);
            }

            var credential = url.GetCredential();

            var clientSettings = new MongoClientSettings();
            clientSettings.AllowInsecureTls = url.AllowInsecureTls;
            clientSettings.ApplicationName = url.ApplicationName;
            clientSettings.AutoEncryptionOptions = null; // must be configured via code
            clientSettings.Compressors = url.Compressors;
#pragma warning disable CS0618 // Type or member is obsolete
            if (url.ConnectionModeSwitch == ConnectionModeSwitch.UseConnectionMode)
            {
                clientSettings.ConnectionMode = url.ConnectionMode;
#pragma warning restore CS0618 // Type or member is obsolete
            }
            clientSettings.ConnectTimeout = url.ConnectTimeout;
            if (credential != null)
            {
                foreach (var property in url.AuthenticationMechanismProperties)
                {
                    if (property.Key.Equals("CANONICALIZE_HOST_NAME", StringComparison.OrdinalIgnoreCase))
                    {
                        credential = credential.WithMechanismProperty(property.Key, bool.Parse(property.Value));
                    }
                    else
                    {
                        credential = credential.WithMechanismProperty(property.Key, property.Value);
                    }
                }
                clientSettings.Credential = credential;
            }
#pragma warning disable CS0618 // Type or member is obsolete
            if (url.ConnectionModeSwitch == ConnectionModeSwitch.UseDirectConnection)
#pragma warning restore CS0618 // Type or member is obsolete
            {
                clientSettings.DirectConnection = url.DirectConnection;
            }
#pragma warning disable 618
            if (BsonDefaults.GuidRepresentationMode == GuidRepresentationMode.V2)
            {
                clientSettings.GuidRepresentation = url.GuidRepresentation;
            }
#pragma warning restore 618
            clientSettings.HeartbeatInterval = url.HeartbeatInterval;
            clientSettings.HeartbeatTimeout = url.HeartbeatTimeout;
            clientSettings.IPv6 = url.IPv6;
            clientSettings.LinqProvider = LinqProvider.V3;
            clientSettings.LoadBalanced = url.LoadBalanced;
            clientSettings.LocalThreshold = url.LocalThreshold;
            clientSettings.MaxConnecting = url.MaxConnecting;
            clientSettings.MaxConnectionIdleTime = url.MaxConnectionIdleTime;
            clientSettings.MaxConnectionLifeTime = url.MaxConnectionLifeTime;
            clientSettings.MaxConnectionPoolSize = ConnectionStringConversions.GetEffectiveMaxConnections(url.MaxConnectionPoolSize);
            clientSettings.MinConnectionPoolSize = url.MinConnectionPoolSize;
            clientSettings.ReadConcern = new ReadConcern(url.ReadConcernLevel);
            clientSettings.ReadEncoding = null; // ReadEncoding must be provided in code
            clientSettings.ReadPreference = (url.ReadPreference == null) ? ReadPreference.Primary : url.ReadPreference;
            clientSettings.ReplicaSetName = url.ReplicaSetName;
            clientSettings.RetryReads = url.RetryReads.GetValueOrDefault(true);
            clientSettings.RetryWrites = url.RetryWrites.GetValueOrDefault(true);
            clientSettings.Scheme = url.Scheme;
            clientSettings.Servers = new List<MongoServerAddress>(url.Servers);
            clientSettings.ServerSelectionTimeout = url.ServerSelectionTimeout;
            clientSettings.SocketTimeout = url.SocketTimeout;
            clientSettings.SrvMaxHosts = url.SrvMaxHosts.GetValueOrDefault(0);
            clientSettings.SslSettings = null;
            if (url.TlsDisableCertificateRevocationCheck)
            {
                clientSettings.SslSettings = new SslSettings { CheckCertificateRevocation = false };
            }
            clientSettings.UseTls = url.UseTls;
#pragma warning disable 618
            clientSettings.WaitQueueSize = url.ComputedWaitQueueSize;
#pragma warning restore 618
            clientSettings.WaitQueueTimeout = url.WaitQueueTimeout;
            clientSettings.WriteConcern = url.GetWriteConcern(true); // WriteConcern is enabled by default for MongoClient
            clientSettings.WriteEncoding = null; // WriteEncoding must be provided in code
            return clientSettings;
        }

        // public methods
        /// <summary>
        /// Creates a clone of the settings.
        /// </summary>
        /// <returns>A clone of the settings.</returns>
        public MongoClientSettings Clone()
        {
            var clone = new MongoClientSettings();
            clone._allowInsecureTls = _allowInsecureTls;
            clone._applicationName = _applicationName;
            clone._autoEncryptionOptions = _autoEncryptionOptions;
            clone._compressors = _compressors;
            clone._clusterConfigurator = _clusterConfigurator;
            clone._connectionMode = _connectionMode;
            clone._connectionModeSwitch = _connectionModeSwitch;
            clone._connectTimeout = _connectTimeout;
            clone._credentials = _credentials;
            clone._directConnection = _directConnection;
            clone._guidRepresentation = _guidRepresentation;
            clone._heartbeatInterval = _heartbeatInterval;
            clone._heartbeatTimeout = _heartbeatTimeout;
            clone._ipv6 = _ipv6;
            clone._linqProvider = _linqProvider;
            clone._loadBalanced = _loadBalanced;
            clone._localThreshold = _localThreshold;
            clone._loggingSettings = _loggingSettings;
            clone._maxConnecting = _maxConnecting;
            clone._maxConnectionIdleTime = _maxConnectionIdleTime;
            clone._maxConnectionLifeTime = _maxConnectionLifeTime;
            clone._maxConnectionPoolSize = _maxConnectionPoolSize;
            clone._minConnectionPoolSize = _minConnectionPoolSize;
            clone._readConcern = _readConcern;
            clone._readEncoding = _readEncoding;
            clone._readPreference = _readPreference;
            clone._replicaSetName = _replicaSetName;
            clone._retryReads = _retryReads;
            clone._retryWrites = _retryWrites;
            clone._scheme = _scheme;
            clone._sdamLogFilename = _sdamLogFilename;
            clone._serverApi = _serverApi;
            clone._servers = new List<MongoServerAddress>(_servers);
            clone._serverSelectionTimeout = _serverSelectionTimeout;
            clone._socketTimeout = _socketTimeout;
            clone._srvMaxHosts = _srvMaxHosts;
            clone._sslSettings = (_sslSettings == null) ? null : _sslSettings.Clone();
            clone._useTls = _useTls;
            clone._waitQueueSize = _waitQueueSize;
            clone._waitQueueTimeout = _waitQueueTimeout;
            clone._writeConcern = _writeConcern;
            clone._writeEncoding = _writeEncoding;
            return clone;
        }

        /// <summary>
        /// Determines whether the specified <see cref="MongoClientSettings" /> is equal to this instance.
        /// </summary>
        /// <param name="obj">The <see cref="MongoClientSettings" /> to compare with this instance.</param>
        /// <returns>
        ///   <c>true</c> if the specified <see cref="MongoClientSettings" /> is equal to this instance; otherwise, <c>false</c>.
        /// </returns>
        public bool Equals(MongoClientSettings obj)
        {
            return Equals((object)obj); // handles obj == null correctly
        }

        /// <summary>
        /// Determines whether the specified <see cref="System.Object" /> is equal to this instance.
        /// </summary>
        /// <param name="obj">The <see cref="System.Object" /> to compare with this instance.</param>
        /// <returns>
        ///   <c>true</c> if the specified <see cref="System.Object" /> is equal to this instance; otherwise, <c>false</c>.
        /// </returns>
        public override bool Equals(object obj)
        {
            if (object.ReferenceEquals(obj, null) || GetType() != obj.GetType()) { return false; }
            var rhs = (MongoClientSettings)obj;
            return
                _allowInsecureTls == rhs._allowInsecureTls &&
                _applicationName == rhs._applicationName &&
                object.Equals(_autoEncryptionOptions, rhs._autoEncryptionOptions) &&
                object.ReferenceEquals(_clusterConfigurator, rhs._clusterConfigurator) &&
                _compressors.SequenceEqual(rhs._compressors) &&
                _connectionMode == rhs._connectionMode &&
                _connectionModeSwitch == rhs._connectionModeSwitch &&
                _connectTimeout == rhs._connectTimeout &&
                _credentials == rhs._credentials &&
                _directConnection.Equals(rhs._directConnection) &&
                _guidRepresentation == rhs._guidRepresentation &&
                _heartbeatInterval == rhs._heartbeatInterval &&
                _heartbeatTimeout == rhs._heartbeatTimeout &&
                _ipv6 == rhs._ipv6 &&
                _linqProvider == rhs._linqProvider &&
                _loadBalanced == rhs._loadBalanced &&
                _localThreshold == rhs._localThreshold &&
                _loggingSettings == rhs._loggingSettings &&
                _maxConnecting == rhs._maxConnecting &&
                _maxConnectionIdleTime == rhs._maxConnectionIdleTime &&
                _maxConnectionLifeTime == rhs._maxConnectionLifeTime &&
                _maxConnectionPoolSize == rhs._maxConnectionPoolSize &&
                _minConnectionPoolSize == rhs._minConnectionPoolSize &&
                object.Equals(_readEncoding, rhs._readEncoding) &&
                object.Equals(_readConcern, rhs._readConcern) &&
                object.Equals(_readPreference, rhs._readPreference) &&
                _replicaSetName == rhs._replicaSetName &&
                _retryReads == rhs._retryReads &&
                _retryWrites == rhs._retryWrites &&
                _scheme == rhs._scheme &&
                _sdamLogFilename == rhs._sdamLogFilename &&
                _serverApi == rhs._serverApi &&
                _servers.SequenceEqual(rhs._servers) &&
                _serverSelectionTimeout == rhs._serverSelectionTimeout &&
                _socketTimeout == rhs._socketTimeout &&
                _srvMaxHosts == rhs._srvMaxHosts &&
                _sslSettings == rhs._sslSettings &&
                _useTls == rhs._useTls &&
                _waitQueueSize == rhs._waitQueueSize &&
                _waitQueueTimeout == rhs._waitQueueTimeout &&
                object.Equals(_writeConcern, rhs._writeConcern) &&
                object.Equals(_writeEncoding, rhs._writeEncoding);
        }

        /// <summary>
        /// Freezes the settings.
        /// </summary>
        /// <returns>The frozen settings.</returns>
        public MongoClientSettings Freeze()
        {
            if (!_isFrozen)
            {
                ThrowIfSettingsAreInvalid();
                _frozenHashCode = GetHashCode();
                _frozenStringRepresentation = ToString();
                _isFrozen = true;
            }
            return this;
        }

        /// <summary>
        /// Returns a frozen copy of the settings.
        /// </summary>
        /// <returns>A frozen copy of the settings.</returns>
        public MongoClientSettings FrozenCopy()
        {
            if (_isFrozen)
            {
                return this;
            }
            else
            {
                return Clone().Freeze();
            }
        }

        /// <summary>
        /// Gets the hash code.
        /// </summary>
        /// <returns>The hash code.</returns>
        public override int GetHashCode()
        {
            if (_isFrozen)
            {
                return _frozenHashCode;
            }

            return new Hasher()
                .Hash(_allowInsecureTls)
                .Hash(_applicationName)
                .Hash(_autoEncryptionOptions)
                .Hash(_clusterConfigurator)
                .HashElements(_compressors)
                .Hash(_connectionMode)
                .Hash(_connectTimeout)
                .Hash(_credentials)
                .Hash(_directConnection)
                .Hash(_guidRepresentation)
                .Hash(_heartbeatInterval)
                .Hash(_heartbeatTimeout)
                .Hash(_ipv6)
                .Hash(_loadBalanced)
                .Hash(_localThreshold)
                .Hash(_maxConnecting)
                .Hash(_maxConnectionIdleTime)
                .Hash(_maxConnectionLifeTime)
                .Hash(_maxConnectionPoolSize)
                .Hash(_minConnectionPoolSize)
                .Hash(_readConcern)
                .Hash(_readEncoding)
                .Hash(_readPreference)
                .Hash(_replicaSetName)
                .Hash(_retryReads)
                .Hash(_retryWrites)
                .Hash(_scheme)
                .Hash(_sdamLogFilename)
                .Hash(_serverApi)
                .HashElements(_servers)
                .Hash(_serverSelectionTimeout)
                .Hash(_socketTimeout)
                .Hash(_srvMaxHosts)
                .Hash(_sslSettings)
                .Hash(_useTls)
                .Hash(_waitQueueSize)
                .Hash(_waitQueueTimeout)
                .Hash(_writeConcern)
                .Hash(_writeEncoding)
                .GetHashCode();
        }

        /// <summary>
        /// Returns a string representation of the settings.
        /// </summary>
        /// <returns>A string representation of the settings.</returns>
        public override string ToString()
        {
            if (_isFrozen)
            {
                return _frozenStringRepresentation;
            }

            var sb = new StringBuilder();
            if (_applicationName != null)
            {
                sb.AppendFormat("ApplicationName={0};", _applicationName);
            }
            if (_autoEncryptionOptions != null)
            {
                sb.AppendFormat("AutoEncryptionOptions={0};", _autoEncryptionOptions);
            }
            if (_compressors?.Any() ?? false)
            {
                sb.AppendFormat("Compressors=[{0}];", string.Join(",", _compressors));
            }
            if (_connectionModeSwitch == ConnectionModeSwitch.UseConnectionMode)
            {
                sb.AppendFormat("ConnectionMode={0};", _connectionMode);
            }
            sb.AppendFormat("ConnectTimeout={0};", _connectTimeout);
            sb.AppendFormat("Credentials={{{0}}};", _credentials);
            if (_connectionModeSwitch == ConnectionModeSwitch.UseDirectConnection && _directConnection.HasValue)
            {
                sb.AppendFormat("DirectConnection={0};", _directConnection.Value);
            }
            sb.AppendFormat("GuidRepresentation={0};", _guidRepresentation);
            sb.AppendFormat("HeartbeatInterval={0};", _heartbeatInterval);
            sb.AppendFormat("HeartbeatTimeout={0};", _heartbeatTimeout);
            sb.AppendFormat("IPv6={0};", _ipv6);
            sb.AppendFormat("LinqProvider={0};", _linqProvider);
            if (_loadBalanced)
            {
                sb.AppendFormat("LoadBalanced={0};", _loadBalanced);
            }
            sb.AppendFormat("LocalThreshold={0};", _localThreshold);
            sb.AppendFormat("MaxConnecting={0};", _maxConnecting);
            sb.AppendFormat("MaxConnectionIdleTime={0};", _maxConnectionIdleTime);
            sb.AppendFormat("MaxConnectionLifeTime={0};", _maxConnectionLifeTime);
            sb.AppendFormat("MaxConnectionPoolSize={0};", _maxConnectionPoolSize);
            sb.AppendFormat("MinConnectionPoolSize={0};", _minConnectionPoolSize);
            if (_readEncoding != null)
            {
                sb.Append("ReadEncoding=UTF8Encoding;");
            }
            sb.AppendFormat("ReadConcern={0};", _readConcern);
            sb.AppendFormat("ReadPreference={0};", _readPreference);
            sb.AppendFormat("ReplicaSetName={0};", _replicaSetName);
            sb.AppendFormat("RetryReads={0}", _retryReads);
            sb.AppendFormat("RetryWrites={0}", _retryWrites);
            if (_scheme != ConnectionStringScheme.MongoDB)
            {
                sb.AppendFormat("Scheme={0};", _scheme);
            }
            if (_sdamLogFilename != null)
            {
                sb.AppendFormat("SDAMLogFileName={0};", _sdamLogFilename);
            }
            if (_serverApi != null)
            {
                sb.AppendFormat("ServerApi={0};", _serverApi);
            }
            sb.AppendFormat("Servers={0};", string.Join(",", _servers.Select(s => s.ToString()).ToArray()));
            sb.AppendFormat("ServerSelectionTimeout={0};", _serverSelectionTimeout);
            sb.AppendFormat("SocketTimeout={0};", _socketTimeout);
            sb.AppendFormat("SrvMaxHosts={0}", _srvMaxHosts);
            if (_sslSettings != null)
            {
                sb.AppendFormat("SslSettings={0};", _sslSettings);
            }
            sb.AppendFormat("Tls={0};", _useTls);
            sb.AppendFormat("TlsInsecure={0};", _allowInsecureTls);
            sb.AppendFormat("WaitQueueSize={0};", _waitQueueSize);
            sb.AppendFormat("WaitQueueTimeout={0}", _waitQueueTimeout);
            sb.AppendFormat("WriteConcern={0};", _writeConcern);
            if (_writeEncoding != null)
            {
                sb.Append("WriteEncoding=UTF8Encoding;");
            }
            return sb.ToString();
        }

        // internal methods
        internal ClusterKey ToClusterKey()
        {
            return new ClusterKey(
                _allowInsecureTls,
                _applicationName,
                _clusterConfigurator,
                _compressors,
                _connectionMode,
                _connectionModeSwitch,
                _connectTimeout,
                _credentials.ToList(),
                _autoEncryptionOptions?.ToCryptClientSettings(),
                _directConnection,
                _heartbeatInterval,
                _heartbeatTimeout,
                _ipv6,
                _loadBalanced,
                _localThreshold,
                _loggingSettings,
                _maxConnecting,
                _maxConnectionIdleTime,
                _maxConnectionLifeTime,
                _maxConnectionPoolSize,
                _minConnectionPoolSize,
                MongoDefaults.TcpReceiveBufferSize, // TODO: add ReceiveBufferSize to MongoClientSettings?
                _replicaSetName,
                _scheme,
                _sdamLogFilename,
                MongoDefaults.TcpSendBufferSize, // TODO: add SendBufferSize to MongoClientSettings?
                _serverApi,
                _servers.ToList(),
                _serverSelectionTimeout,
                _socketTimeout,
                _srvMaxHosts,
                _sslSettings,
                _useTls,
                _waitQueueSize,
                _waitQueueTimeout);
        }

        // private methods
        private void ThrowIfFrozen()
        {
            if (_isFrozen)
            {
                throw new InvalidOperationException($"{nameof(MongoClientSettings)} is frozen.");
            }
        }

        private void ThrowIfSettingsAreInvalid()
        {
            if (_allowInsecureTls && _sslSettings != null && _sslSettings.CheckCertificateRevocation)
            {
                throw new InvalidOperationException(
                        $"{nameof(AllowInsecureTls)} and {nameof(SslSettings)}" +
                        $".{nameof(_sslSettings.CheckCertificateRevocation)} cannot both be true.");
            }

            if (IsDirectConnection())
            {
                if (_scheme == ConnectionStringScheme.MongoDBPlusSrv)
                {
                    throw new InvalidOperationException($"SRV cannot be used with direct connections.");
                }

                if (_servers.Count > 1)
                {
                    throw new InvalidOperationException($"Multiple host names cannot be used with direct connections.");
                }
            }

            if (_srvMaxHosts > 0 && _scheme != ConnectionStringScheme.MongoDBPlusSrv)
            {
                throw new InvalidOperationException("srvMaxHosts can only be used with the mongodb+srv scheme.");
            }

            if (_replicaSetName != null && _srvMaxHosts > 0)
            {
                throw new InvalidOperationException("Specifying srvMaxHosts when connecting to a replica set is invalid.");
            }

            if (_loadBalanced)
            {
                if (_servers.Count > 1)
                {
                    throw new InvalidOperationException("Load balanced mode cannot be used with multiple host names.");
                }

                if (_replicaSetName != null)
                {
                    throw new InvalidOperationException("ReplicaSetName cannot be used with load balanced mode.");
                }

                if (_srvMaxHosts > 0)
                {
                    throw new InvalidOperationException("srvMaxHosts cannot be used with load balanced mode.");
                }

                if (IsDirectConnection())
                {
                    throw new InvalidOperationException("Load balanced mode cannot be used with direct connection.");
                }
            }

            bool IsDirectConnection()
            {
                if (_connectionModeSwitch == ConnectionModeSwitch.UseDirectConnection)
                {
                    return _directConnection.GetValueOrDefault();
                }
                else
                {
                    return _connectionMode == ConnectionMode.Direct;
                }
            }
        }
    }
}
