﻿using Dapper;
using DevChatter.DevStreams.Core;
using DevChatter.DevStreams.Core.Model;
using DevChatter.DevStreams.Core.Services;
using DevChatter.DevStreams.Core.Settings;
using Microsoft.Extensions.Options;
using NodaTime;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;

namespace DevChatter.DevStreams.Infra.Dapper.Services
{
    public class DapperSessionLookup : IStreamSessionService
    {
        private readonly DatabaseSettings _dbSettings;

        public DapperSessionLookup(IOptions<DatabaseSettings> databaseSettings)
        {
            _dbSettings = databaseSettings.Value;
        }

        public async Task<List<EventResult>> Get(string timeZoneId, DateTime localDateTime, IEnumerable<int> includedTagIds)
        {

            DateTimeZone zone = DateTimeZoneProviders.Tzdb[timeZoneId];
            LocalDate localDate = LocalDate.FromDateTime(localDateTime);

            (DateTime dayStart, DateTime dayEnd) = ResolveDayRange(localDate, zone);

            const string sessionSql = @"SELECT * FROM [StreamSessions]
                WHERE UtcEndTime > @dayStart 
                    AND UtcStartTime < @dayEnd";

            const string channelSql = "SELECT * FROM Channels WHERE Id IN @ids";


            using (IDbConnection connection = new SqlConnection(_dbSettings.DefaultConnection))
            {
                try
                {
                    var args = new { dayStart, dayEnd };
                    var sessions = (await connection.QueryAsync<StreamSession>(sessionSql, args))
                        .ToList();
                    var channelArgs = new { ids = sessions.Select(x => x.ChannelId).ToArray() };
                    var channels = connection.Query<Channel>(channelSql, channelArgs);

                    return sessions
                        .Select(s => new EventResult
                        {
                            StreamSession = s,
                            Channel = channels.Single(c => c.Id == s.ChannelId)
                        })
                        .ToList();
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                    throw;
                }
            }
        }

        private static (DateTime start, DateTime end) ResolveDayRange(LocalDate input,
            DateTimeZone zone)
        {
            Instant dayStart = input.AtStartOfDayInZone(zone).ToInstant();
            Instant dayEnd = input.PlusDays(1).AtStartOfDayInZone(zone).ToInstant();
            return (dayStart.ToDateTimeUtc(), dayEnd.ToDateTimeUtc());
        }

    }
}
