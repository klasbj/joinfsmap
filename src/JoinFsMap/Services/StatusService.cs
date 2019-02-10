using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using JoinFsMap.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace JoinFsMap.Services {
    public class Context {
        public Context(DateTime timeStamp, IEnumerable<Entity> entities) {
            TimeStamp = timeStamp;
            Atcs = entities.Select(x => x as Atc).Where(x => x != null).ToList();
            Pilots = entities.Select(x => x as Pilot).Where(x => x != null).ToList();
        }

        public DateTime TimeStamp { get; }

        public IReadOnlyCollection<Atc> Atcs { get; }

        public IReadOnlyCollection<Pilot> Pilots { get; }
    }

    public interface IStatusContext {
        Context GetContext();

        void SetContext(Context newContext);
    }

    public class StatusContext : IStatusContext {
        private object @lock = new object();
        private Context currentContext = new Context(DateTime.MinValue, Enumerable.Empty<Entity>());

        public Context GetContext() {
            lock (@lock) {
                return currentContext;
            }
        }

        public void SetContext(Context newContext) {
            lock (@lock) {
                currentContext = newContext;
            }
        }
    }

    public class StatusService : BackgroundService {
        private IStatusContext contextManager;
        private ILogger logger;
        private IConfiguration configuration;

        public StatusService(IStatusContext contextManager, ILogger<StatusService> logger, IConfiguration configuration) {
            this.contextManager = contextManager;
            this.logger = logger;
            this.configuration = configuration;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken) {
            var whazzupFile = configuration.GetValue("WhazzupFile", "whazzup.txt");
            logger.LogInformation($"Starting {nameof(StatusService)}, reading {whazzupFile}.");

            bool anyClients = false;

            while (!stoppingToken.IsCancellationRequested) {
                try {
                    WhazzupData newData = null;
                    IWhazzupReader reader = new WhazzupReader();

                    using (var file = System.IO.File.OpenRead(whazzupFile))
                    using (var fileReader = new StreamReader(file)) {
                        newData = await reader.ReadAsync(fileReader);
                    }

                    Context prevContext = contextManager.GetContext();
                    if (prevContext.TimeStamp < newData.TimeStamp) {
                        var entities = newData.Entities.ToList();
                        var pilots = entities.Where(x => x is Pilot).ToDictionary(k => k.Key, e => (Pilot)e);

                        foreach (var pilot in prevContext.Pilots) {
                            if (pilots.TryGetValue(pilot.Key, out var nPilot)) {
                                nPilot.Trail = pilot.Trail.Prepend(new LoggedPosition {
                                    TimeStamp = prevContext.TimeStamp,
                                    Position = pilot.Position
                                }).ToList();
                            }
                        }

                        Context newContext = new Context(newData.TimeStamp, newData.Entities);

                        anyClients = newContext.Pilots.Count + newContext.Atcs.Count > 0;
                        contextManager.SetContext(newContext);
                    }
                }
                catch (Exception e) {
                    logger.LogError(e, "Error occurred while parsing whazzup.txt file.");
                }

                await Task.Delay(anyClients ? 5000 : 60000, stoppingToken);
            }

            logger.LogInformation($"Shutting down {nameof(StatusService)}.");
        }
    }
}