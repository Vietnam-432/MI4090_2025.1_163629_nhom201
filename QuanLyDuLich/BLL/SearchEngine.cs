using DAL;
using Dapper;

using DTO;
namespace BUS
{
    public static class SearchEngine
    {
        public enum Op
        {
            Like,
            Equal,
            Greater,
            GreaterOrEqual,
            Less,
            LessOrEqual
        }
        public class Condition
        {
            public string Field { get; set; }
            public Op Operator { get; set; }
            public object Value { get; set; }
        }
        public enum SortDir
        {
            Asc,
            Desc
        }
        public class SortOption
        {
            public string Field { get; set; }   // Tên cột
            public SortDir Direction { get; set; }
        }
        public static IEnumerable<T> Search<T>(List<Condition> conditions, SortOption? sort = null) where T : class
        {
            string table = typeof(T).Name + "s";

            var where = new List<string>();
            var param = new DynamicParameters();
            int i = 0;
            if (table == "Users" && !UserSession.Instance.IsAdmin)
            {
                throw new Exception("Không có quyền truy cập");
            }
            else
            {
                foreach (var c in conditions)
                {
                    string p = $"@p{i++}";

                    switch (c.Operator)
                    {
                        case Op.Like:
                            where.Add($"{c.Field} LIKE {p}");
                            param.Add(p, $"%{c.Value}%");
                            break;
                        case Op.Equal:
                            where.Add($"{c.Field} = {p}");
                            param.Add(p, c.Value);
                            break;
                        case Op.Greater:
                            where.Add($"{c.Field} > {p}");
                            param.Add(p, c.Value);
                            break;
                        case Op.GreaterOrEqual:
                            where.Add($"{c.Field} >= {p}");
                            param.Add(p, c.Value);
                            break;
                        case Op.Less:
                            where.Add($"{c.Field} < {p}");
                            param.Add(p, c.Value);
                            break;
                        case Op.LessOrEqual:
                            where.Add($"{c.Field} <= {p}");
                            param.Add(p, c.Value);
                            break;
                    }
                }

                string whereSql = where.Any()
                    ? " WHERE " + string.Join(" AND ", where)
                    : "";

                string orderSql = "";
                if (sort != null)
                {
                    orderSql = $" ORDER BY {sort.Field} {(sort.Direction == SortDir.Asc ? "ASC" : "DESC")}";
                }

                string sql = $"SELECT * FROM {table}{whereSql}{orderSql}";

                return Repository.Instance.Search<T>(sql, param);


            }

        }
    }
}
