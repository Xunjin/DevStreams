﻿using Dapper;
using NodaTime;
using System;
using System.Data;
using System.Data.SqlClient;
using NodaTime.Extensions;

namespace DevChatter.DevStreams.Infra.Dapper.TypeHandlers
{
    public class InstantHandler : SqlMapper.TypeHandler<Instant>
    {
        private InstantHandler()
        {
        }

        public static readonly InstantHandler Default = new InstantHandler();

        public override void SetValue(IDbDataParameter parameter, Instant value)
        {
            parameter.Value = value.ToDateTimeUtc();

            if (parameter is SqlParameter sqlParameter)
            {
                sqlParameter.SqlDbType = SqlDbType.DateTime;
            }
        }

        public override Instant Parse(object value)
        {
            if (value is long unixTicks)
            {
                return Instant.FromUnixTimeTicks(unixTicks);
            }

            if (value is DateTime dateTime)
            {
                return DateTime.SpecifyKind(dateTime, DateTimeKind.Utc).ToInstant();
            }

            if (value is DateTimeOffset dateTimeOffset)
            {
                return Instant.FromDateTimeOffset(dateTimeOffset);
            }

            throw new DataException($"Cannot convert {value.GetType()} to {typeof(Instant).FullName}");
        }
    }
}