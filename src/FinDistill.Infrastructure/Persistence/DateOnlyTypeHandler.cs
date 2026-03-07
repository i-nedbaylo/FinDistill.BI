using System.Data;
using Dapper;

namespace FinDistill.Infrastructure.Persistence;

/// <summary>
/// Dapper type handler for DateOnly ↔ DateTime conversion.
/// Required because Dapper does not natively support DateOnly.
/// </summary>
public class DateOnlyTypeHandler : SqlMapper.TypeHandler<DateOnly>
{
    public override DateOnly Parse(object value) =>
        DateOnly.FromDateTime((DateTime)value);

    public override void SetValue(IDbDataParameter parameter, DateOnly value)
    {
        parameter.DbType = DbType.Date;
        parameter.Value = value.ToDateTime(TimeOnly.MinValue);
    }
}
