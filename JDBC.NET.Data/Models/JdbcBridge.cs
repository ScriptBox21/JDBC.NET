﻿using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using Grpc.Core;
using J2NET;
using JDBC.NET.Data.Utilities;
using JDBC.NET.Proto;

namespace JDBC.NET.Data.Models
{
    internal sealed class JdbcBridge : IDisposable
    {
        #region Fields
        private Channel _channel;
        private Process _process;
        #endregion

        #region Constants
        private const string host = "127.0.0.1";
#if !DEBUG
        private const string jarPath = @"JDBC.NET.Bridge.jar";
#else
        private readonly string jarPath = Path.Combine("..", "..", "..", "..", "JDBC.NET.Bridge", "target", "JDBC.NET.Bridge-1.0-SNAPSHOT-jar-with-dependencies.jar");
#endif
        #endregion

        #region Properties
        public DriverService.DriverServiceClient Driver { get; private set; }

        public ReaderService.ReaderServiceClient Reader { get; private set; }

        public StatementService.StatementServiceClient Statement { get; private set; }

        public DatabaseService.DatabaseServiceClient Database { get; private set; }

        public string Key => GenerateKey(DriverPath, DriverClass);

        public string DriverPath { get; }

        public string DriverClass { get; }

        public int DriverMajorVersion { get; private set; }

        public int DriverMinorVersion { get; private set; }
        #endregion

        #region Constructor
        private JdbcBridge(string driverPath, string driverClass)
        {
            DriverPath = driverPath;
            DriverClass = driverClass;
        }
        #endregion

        #region Public Methods
        internal static string GenerateKey(string driverPath, string driverClass)
        {
            return $"{driverPath}:{driverClass}";
        }

        internal static JdbcBridge FromDriver(string driverPath, string driverClass)
        {
            var bridge = new JdbcBridge(driverPath, driverClass);
            bridge.Initialize();

            return bridge;
        }
        #endregion

        #region Private Methods
        private void Initialize()
        {
            var port = PortUtility.GetFreeTcpPort();

            // TODO : Need to move Execute logic to J2NET
            var classPaths = string.Join(RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? ";" : ":", jarPath, DriverPath);
            _process = JavaRuntime.Execute($"-XX:G1PeriodicGCInterval=5000 -cp \"{classPaths}\" com.chequer.jdbcnet.bridge.Main -p {port}");
            PortUtility.WaitForOpen(port);

            _channel = new Channel($"{host}:{port}", ChannelCredentials.Insecure);

            Driver = new DriverService.DriverServiceClient(_channel);
            Reader = new ReaderService.ReaderServiceClient(_channel);
            Statement = new StatementService.StatementServiceClient(_channel);
            Database = new DatabaseService.DatabaseServiceClient(_channel);

            var loadDriverResponse = Driver.loadDriver(new LoadDriverRequest
            {
                ClassName = DriverClass
            });

            DriverMajorVersion = loadDriverResponse.MajorVersion;
            DriverMinorVersion = loadDriverResponse.MinorVersion;
        }
        #endregion

        #region IDisposable
        public void Dispose()
        {
            _process?.Kill();
            _process?.Dispose();
        }
        #endregion
    }
}
