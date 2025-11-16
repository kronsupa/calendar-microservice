using System.Reflection;
using System.Text;
using MySql.Data.MySqlClient;
using System.Data;

public class DataService
{
    public string ConnectionString { get; set; }

    public DataService(string connectionString)
    {
        ConnectionString = connectionString;
    }

    public List<T>? Select<T>(T? model)
    {
        try
        {
            using (MySqlConnection connection = new MySqlConnection(ConnectionString))
            {
                connection.Open();

                // Get the name of the table to query
                DataModelAttribute? ModelAttribute = typeof(T).GetCustomAttribute<DataModelAttribute>();
                if (ModelAttribute == null)
                {
                    throw new Exception("Invalid DataModel Provided");
                }

                string tableName = ModelAttribute.TableName;

                StringBuilder builder = new StringBuilder();
                Dictionary<string, object?> parameterDict = new Dictionary<string, object?>();
                builder.Append($"select * from {tableName}");

                if (model != null)
                {
                    // Form the request
                    bool trigger = false;
                    foreach (PropertyInfo info in typeof(T).GetProperties())
                    {
                        if (info.GetValue(model) != null)
                        {
                            ColumnAttribute? columnAttribute = info.GetCustomAttribute<ColumnAttribute>();
                            if (columnAttribute == null)
                            {
                                throw new Exception("Invalid DataModel Provided");
                            }

                            if (!trigger)
                            {
                                builder.Append(" where ");
                                trigger = true;
                            }
                            else
                                builder.Append(" and ");

                            string paramName = Guid.NewGuid().ToString().Replace("-", String.Empty);
                            builder.Append($"{columnAttribute.FieldName} = @{paramName}");
                            parameterDict.Add($"@{paramName}", info.GetValue(model));
                        }
                    }
                }

                builder.Append(";");

                MySqlCommand command = connection.CreateCommand();
                command.CommandText = builder.ToString();

                foreach (string parameter in parameterDict.Keys)
                {
                    command.Parameters.AddWithValue(parameter, parameterDict[parameter]);
                }

                MySqlDataReader reader = command.ExecuteReader();

                // For each properties, use the selector to select from the database
                PropertyInfo[] propertyInfos = typeof(T).GetProperties();

                List<T> modelList = new List<T>();

                while (reader.Read())
                {
                    T? instance = (T?)typeof(T).GetConstructors().FirstOrDefault()?.Invoke(null);
                    if (instance == null)
                        throw new Exception("Model Instance Null");
                    foreach (PropertyInfo info in propertyInfos)
                    {
                        ColumnAttribute? columnAttribute = info.GetCustomAttribute<ColumnAttribute>();
                        if (columnAttribute == null)
                        {
                            throw new Exception("Invalid DataModel Provided");
                        }

                        Type type = Nullable.GetUnderlyingType(info.PropertyType) ?? info.PropertyType;

                        info.SetValue(instance, Convert.ChangeType(reader.GetFieldValue<object>(columnAttribute.FieldName), type));
                    }

                    modelList.Add(instance);
                }

                return modelList;
            }
        }
        catch (Exception e)
        {
            Console.WriteLine(e.ToString());
            return null;
        }
    }

    public void Insert<T>(T model)
    {
        try
        {
            using (MySqlConnection connection = new MySqlConnection(ConnectionString))
            {
                connection.Open();

                if (model == null) throw new ArgumentException("NUll Model To Insert");

                // Get the name of the table to query
                DataModelAttribute? ModelAttribute = typeof(T).GetCustomAttribute<DataModelAttribute>();
                if (ModelAttribute == null)
                {
                    throw new Exception("Invalid DataModel Provided");
                }

                string tableName = ModelAttribute.TableName;


                StringBuilder fields = new StringBuilder();
                StringBuilder values = new StringBuilder();
                Dictionary<string, object?> parameterDict = new Dictionary<string, object?>();

                // Form the request
                bool trigger = false;
                foreach (PropertyInfo info in typeof(T).GetProperties())
                {
                    if (info.GetValue(model) != null)
                    {
                        ColumnAttribute? columnAttribute = info.GetCustomAttribute<ColumnAttribute>();
                        if (columnAttribute == null)
                        {
                            throw new Exception("Invalid DataModel Provided");
                        }

                        if (!trigger)
                            trigger = true;
                        else
                        {
                            fields.Append(",");
                            values.Append(",");
                        }

                        fields.Append(columnAttribute.FieldName);

                        string paramName = Guid.NewGuid().ToString().Replace("-", String.Empty);
                        parameterDict.Add($"@{paramName}", info.GetValue(model));
                        values.Append($"@{paramName}");
                    }
                    else
                    {
                        throw new Exception("Null Value Provided");
                    }
                }


                string cmd = $"insert into {tableName} ({fields.ToString()}) values ({values.ToString()});";

                MySqlCommand command = connection.CreateCommand();
                command.CommandText = cmd;

                foreach (string parameter in parameterDict.Keys)
                {
                    command.Parameters.AddWithValue(parameter, parameterDict[parameter]);
                }

                command.ExecuteNonQuery();
            }
        }
        catch (Exception e)
        {
            Console.WriteLine(e.Message);
        }
    }

    public void Update<T>(T model)
    {
        try
        {
            using (MySqlConnection connection = new MySqlConnection(ConnectionString))
            {
                connection.Open();

                // Get the name of the table to query
                DataModelAttribute? ModelAttribute = typeof(T).GetCustomAttribute<DataModelAttribute>();
                if (ModelAttribute == null)
                {
                    throw new Exception("Invalid DataModel Provided");
                }

                PropertyInfo[]? pkeys = typeof(T).GetProperties().Where(p => p.GetCustomAttribute<ColumnAttribute>()?.PrimaryKey == true).ToArray();

                if (pkeys == null || pkeys.Length == 0)
                {
                    throw new Exception("No Primary Key Available To Update");
                }

                string tableName = ModelAttribute.TableName;

                StringBuilder builder = new StringBuilder();
                Dictionary<string, object?> parameterDict = new Dictionary<string, object?>();
                builder.Append($"update {tableName} set ");

                if (model != null)
                {
                    // Form the request
                    bool trigger = false;
                    foreach (PropertyInfo info in typeof(T).GetProperties())
                    {
                        if (pkeys.Contains(info))
                            continue;

                        if (info.GetValue(model) != null)
                        {
                            ColumnAttribute? columnAttribute = info.GetCustomAttribute<ColumnAttribute>();
                            if (columnAttribute == null)
                            {
                                throw new Exception("Invalid DataModel Provided");
                            }

                            if (!trigger)
                            {
                                trigger = true;
                            }
                            else
                                builder.Append(", ");

                            string paramName = Guid.NewGuid().ToString().Replace("-", String.Empty);
                            builder.Append($"{columnAttribute.FieldName} = @{paramName}");
                            parameterDict.Add($"@{paramName}", info.GetValue(model));
                        }
                    }

                    bool trigger2 = false;
                    foreach (PropertyInfo info in pkeys)
                    {
                        if (info.GetValue(model) != null)
                        {
                            ColumnAttribute? columnAttribute = info.GetCustomAttribute<ColumnAttribute>();
                            if (columnAttribute == null)
                            {
                                throw new Exception("Invalid DataModel Provided");
                            }

                            if (!trigger2)
                            {
                                builder.Append(" where ");
                                trigger2 = true;
                            }
                            else
                                builder.Append(" and ");

                            string paramName = Guid.NewGuid().ToString().Replace("-", String.Empty);
                            builder.Append($"{columnAttribute.FieldName} = @{paramName}");
                            parameterDict.Add($"@{paramName}", info.GetValue(model));
                        }
                    }
                }

                builder.Append(";");

                MySqlCommand command = connection.CreateCommand();
                command.CommandText = builder.ToString();

                foreach (string parameter in parameterDict.Keys)
                {
                    command.Parameters.AddWithValue(parameter, parameterDict[parameter]);
                }

                command.ExecuteNonQuery();
            }
        }
        catch (Exception e)
        {
            Console.WriteLine(e.Message);
        }
    }

    public void Delete<T>(T model)
    {
        try
        {
            using (MySqlConnection connection = new MySqlConnection(ConnectionString))
            {
                connection.Open();

                if (model == null)
                    throw new Exception("Invalid Model Provided");

                // Get the name of the table to query
                DataModelAttribute? ModelAttribute = typeof(T).GetCustomAttribute<DataModelAttribute>();
                if (ModelAttribute == null)
                {
                    throw new Exception("Invalid DataModel Provided");
                }

                PropertyInfo[]? pkeys = typeof(T).GetProperties().Where(p => p.GetCustomAttribute<ColumnAttribute>()?.PrimaryKey == true).ToArray();

                if (pkeys == null || pkeys.Length == 0)
                {
                    throw new Exception("No Primary Key Available To Update");
                }

                string tableName = ModelAttribute.TableName;

                StringBuilder builder = new StringBuilder();
                Dictionary<string, object?> parameterDict = new Dictionary<string, object?>();

                builder.Append($"delete from {tableName}");

                bool trigger = false;
                foreach (PropertyInfo info in pkeys)
                {
                    if (info.GetValue(model) != null)
                    {
                        ColumnAttribute? columnAttribute = info.GetCustomAttribute<ColumnAttribute>();
                        if (columnAttribute == null)
                        {
                            throw new Exception("Invalid DataModel Provided");
                        }

                        if (!trigger)
                        {
                            builder.Append(" where ");
                            trigger = true;
                        }
                        else
                            builder.Append(" and ");

                        string paramName = Guid.NewGuid().ToString().Replace("-", String.Empty);
                        builder.Append($"{columnAttribute.FieldName} = @{paramName}");
                        parameterDict.Add($"@{paramName}", info.GetValue(model));
                    }
                }


                MySqlCommand command = connection.CreateCommand();
                command.CommandText = builder.ToString();

                foreach (string parameter in parameterDict.Keys)
                {
                    command.Parameters.AddWithValue(parameter, parameterDict[parameter]);
                }

                command.ExecuteNonQuery();
            }
        }
        catch (Exception e)
        {
            Console.WriteLine(e.Message);
        }
    }


    [AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
    public class DataModelAttribute : Attribute
    {
        public string TableName { get; set; }

        public DataModelAttribute(string name)
        {
            TableName = name;
        }
    }

    [AttributeUsage(AttributeTargets.Property, Inherited = false, AllowMultiple = false)]
    public class ColumnAttribute : Attribute
    {
        public string FieldName { get; set; }
        public bool PrimaryKey { get; set; }

        public ColumnAttribute(string fieldName, bool primary = false)
        {
            FieldName = fieldName;
            PrimaryKey = primary;
        }
    }
}

