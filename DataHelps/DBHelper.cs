using System;
using System.Data;
using System.Data.SqlClient;
using Oracle.ManagedDataAccess.Client;
using System.Collections.Generic;


namespace DataHelps
{
    /// <summary>
    /// Class hỗ trợ các thao tác kết nối và truy vấn trên cơ sở dữ liệu SQL Server và Oracle.
    /// </summary>
    public class DBHelper
    {
        public static string XMLFilePath; // Đường dẫn mặc định của file XML cấu hình

        static DBHelper()
        {
            XMLFilePath = "SysInfo.xml";
        }

        public DBHelper() { }

        public enum LogType : short
        {
            Debug,
            Normal,
            Warning,
            Error
        }

        /// <summary>
        /// Mở kết nối tới Oracle Database với phần cấu hình được chỉ định.
        /// </summary>
        /// <param name="dbSectionName">Tên phần cấu hình trong file XML</param>
        public static OracleConnection OpenOracleConnection(string dbSectionName)
        {
            return OpenOracleConnection(dbSectionName, XMLFilePath);
        }

        /// <summary>
        /// Mở kết nối tới Oracle Database với phần cấu hình và file XML chỉ định.
        /// </summary>
        /// <param name="dbSectionName">Tên phần cấu hình trong file XML</param>
        /// <param name="xmlFilePath">Đường dẫn tới file XML</param>
        public static OracleConnection OpenOracleConnection(string dbSectionName, string xmlFilePath)
        {
            xmlFilePath = string.IsNullOrEmpty(xmlFilePath) ? XMLFilePath : xmlFilePath;

            // Lấy thông tin cấu hình từ file XML
            string dbIP = XMLHelper.GetXMLNodeText(xmlFilePath, dbSectionName, "DBIP");
            string dbName = XMLHelper.GetXMLNodeText(xmlFilePath, dbSectionName, "DBName");
            string userID = XMLHelper.GetXMLNodeText(xmlFilePath, dbSectionName, "UID");
            string password = XMLHelper.GetXMLNodeText(xmlFilePath, dbSectionName, "PWD");
            string port = XMLHelper.GetXMLNodeText(xmlFilePath, dbSectionName, "Port");

            port = string.IsNullOrEmpty(port) ? "1521" : port;
            string oracleDBSource = GetOracleDBSource(dbIP, dbName, int.Parse(port));

            // Tạo kết nối Oracle
            var oracleConnection = new OracleConnection($"Password={password};Persist Security Info=True;User ID={userID};Data Source={oracleDBSource}");
            oracleConnection.Open();
            return oracleConnection;
        }

        /// <summary>
        /// Mở kết nối tới SQL Server Database với cấu hình mặc định.
        /// </summary>
        public static SqlConnection OpenSqlConnection()
        {
            return OpenSqlConnection("", XMLFilePath);
        }

        /// <summary>
        /// Mở kết nối tới SQL Server Database với phần cấu hình được chỉ định.
        /// </summary>
        /// <param name="dbSectionName">Tên phần cấu hình trong file XML</param>
        public static SqlConnection OpenSqlConnection(string dbSectionName)
        {
            return OpenSqlConnection(dbSectionName, XMLFilePath);
        }

        /// <summary>
        /// Mở kết nối tới SQL Server Database với phần cấu hình và file XML chỉ định.
        /// </summary>
        /// <param name="dbSectionName">Tên phần cấu hình trong file XML</param>
        /// <param name="xmlFilePath">Đường dẫn tới file XML</param>
        public static SqlConnection OpenSqlConnection(string dbSectionName, string xmlFilePath)
        {
            xmlFilePath = string.IsNullOrEmpty(xmlFilePath) ? XMLFilePath : xmlFilePath;
            dbSectionName = string.IsNullOrEmpty(dbSectionName) ? XMLHelper.GetXMLNodeText(xmlFilePath, "SystemInfo", "DBSection") : dbSectionName;

            // Lấy thông tin cấu hình từ file XML
            string dbIP = XMLHelper.GetXMLNodeText(xmlFilePath, dbSectionName, "DBIP");
            string dbName = XMLHelper.GetXMLNodeText(xmlFilePath, dbSectionName, "DBName");
            string userID = XMLHelper.GetXMLNodeText(xmlFilePath, dbSectionName, "UID");
            string password = XMLHelper.GetXMLNodeText(xmlFilePath, dbSectionName, "PWD");

            // Tạo kết nối SQL Server
            var sqlConnection = new SqlConnection($"Server={dbIP};User ID={userID};Password={password};Database={dbName}");
            sqlConnection.Open();
            return sqlConnection;
        }

        /// <summary>
        /// Thực thi lệnh SQL không trả về dữ liệu trên SQL Server.
        /// </summary>
        /// <param name="sqlCommandText">Câu lệnh SQL cần thực thi</param>
        public static void ExecuteNonQuerySqlCommand(string sqlCommandText)
        {
            ExecuteNonQuerySqlCommand(null, sqlCommandText);
        }

        /// <summary>
        /// Thực thi lệnh SQL không trả về dữ liệu trên SQL Server với kết nối SQL chỉ định.
        /// </summary>
        public static void ExecuteNonQuerySqlCommand(SqlConnection sqlConnection, string sqlCommandText)
        {
            try
            {
                //sqlConnection ??= OpenSqlConnection();
                if (sqlConnection == null)
                {
                    sqlConnection = OpenSqlConnection();
                }

                new SqlCommand(sqlCommandText, sqlConnection).ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                // Ghi log hoặc xử lý ngoại lệ tại đây nếu cần, sau đó ném lại ngoại lệ
                throw new Exception("Error: " + ex.Message, ex);
            }
            finally
            {
                sqlConnection?.Close();
                sqlConnection?.Dispose();
            }
        }

        /// <summary>
        /// Tạo chuỗi kết nối Oracle từ các tham số.
        /// </summary>
        private static string GetOracleDBSource(string dbIP, string svcName, int port)
        {
            return $"(DESCRIPTION=(ADDRESS_LIST=(ADDRESS=(PROTOCOL=TCP)(HOST={dbIP})(PORT={port})))(CONNECT_DATA=(SERVICE_NAME={svcName})))";
        }



        /// <summary>
        /// Thi hành stored procedure trên SQL Server.
        /// </summary>
        /// <param name="procedureName">Tên của stored procedure</param>
        /// <param name="parameters">Danh sách các tham số truyền vào cho procedure</param>
        public static void ExecuteStoredProcedureSql(string procedureName, Dictionary<string, object> parameters)
        {
            SqlConnection sqlConnection = null;
            SqlCommand command = null;
            try
            {
                sqlConnection = OpenSqlConnection();
                command = new SqlCommand(procedureName, sqlConnection)
                {
                    CommandType = CommandType.StoredProcedure
                };

                // Thêm các tham số vào command
                foreach (var param in parameters)
                {
                    command.Parameters.AddWithValue(param.Key, param.Value ?? DBNull.Value);
                }

                command.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                // Ghi log hoặc xử lý ngoại lệ tại đây nếu cần, sau đó ném lại ngoại lệ
                throw new Exception("Error: " + ex.Message, ex);
            }
            finally
            {
                // Đóng và giải phóng tài nguyên
                if (command != null) command.Dispose();
                if (sqlConnection != null)
                {
                    sqlConnection.Close();
                    sqlConnection.Dispose();
                }
            }
        }

        /// <summary>
        /// Thi hành stored procedure trên Oracle Database.
        /// </summary>
        /// <param name="procedureName">Tên của stored procedure</param>
        /// <param name="parameters">Danh sách các tham số truyền vào cho procedure</param>
        public static void ExecuteStoredProcedureOracle(string procedureName, Dictionary<string, object> parameters)
        {
            OracleConnection oracleConnection = null;
            OracleCommand command = null;
            try
            {
                oracleConnection = OpenOracleConnection("OracleConnection");
                command = new OracleCommand(procedureName, oracleConnection)
                {
                    CommandType = CommandType.StoredProcedure
                };

                // Thêm các tham số vào command
                foreach (var param in parameters)
                {
                    command.Parameters.Add(new OracleParameter(param.Key, param.Value ?? DBNull.Value));
                }

                command.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                // Ghi log hoặc xử lý ngoại lệ tại đây nếu cần, sau đó ném lại ngoại lệ
                throw new Exception("Error: " + ex.Message, ex);
            }
            finally
            {
                // Đóng và giải phóng tài nguyên
                if (command != null) command.Dispose();
                if (oracleConnection != null)
                {
                    oracleConnection.Close();
                    oracleConnection.Dispose();
                }
            }
        }


        /// <summary>
        /// Thi hành stored procedure trên SQL Server và trả về kết quả dưới dạng DataSet.
        /// </summary>
        /// <param name="procedureName">Tên của stored procedure</param>
        /// <param name="parameters">Mảng các tham số chứa hướng, kiểu dữ liệu, tên và giá trị</param>
        /// <returns>DataSet chứa dữ liệu trả về từ stored procedure</returns>
        public static DataSet ExecuteStoredProcedureSql(string procedureName, object[][] parameters)
        {
            SqlConnection sqlConnection = null;
            SqlCommand command = null;
            SqlDataAdapter adapter = null;
            DataSet dataSet = new DataSet();

            try
            {
                sqlConnection = OpenSqlConnection();
                command = new SqlCommand(procedureName, sqlConnection)
                {
                    CommandType = CommandType.StoredProcedure
                };

                // Thêm các tham số vào command
                foreach (var param in parameters)
                {
                    var sqlParameter = new SqlParameter
                    {
                        Direction = (ParameterDirection)param[0],
                        SqlDbType = (SqlDbType)param[1],
                        ParameterName = (string)param[2],
                        Value = param[3] ?? DBNull.Value
                    };
                    command.Parameters.Add(sqlParameter);
                }

                // Sử dụng SqlDataAdapter để điền dữ liệu vào DataSet
                adapter = new SqlDataAdapter(command);
                adapter.Fill(dataSet);
            }
            catch (Exception ex)
            {
                // Ghi log hoặc xử lý ngoại lệ tại đây nếu cần, sau đó ném lại ngoại lệ
                throw new Exception("Error: " + ex.Message, ex);
            }
            finally
            {
                // Đóng và giải phóng tài nguyên
                if (adapter != null) adapter.Dispose();
                if (command != null) command.Dispose();
                if (sqlConnection != null)
                {
                    sqlConnection.Close();
                    sqlConnection.Dispose();
                }
            }

            return dataSet;
        }

        /// <summary>
        /// Thi hành stored procedure trên Oracle Database và trả về kết quả dưới dạng DataSet.
        /// </summary>
        /// <param name="procedureName">Tên của stored procedure</param>
        /// <param name="parameters">Mảng các tham số chứa hướng, kiểu dữ liệu, tên và giá trị</param>
        /// <returns>DataSet chứa dữ liệu trả về từ stored procedure</returns>
        public static DataSet ExecuteStoredProcedureOracle(string procedureName, object[][] parameters)
        {
            OracleConnection oracleConnection = null;
            OracleCommand command = null;
            OracleDataAdapter adapter = null;
            DataSet dataSet = new DataSet();

            try
            {
                oracleConnection = OpenOracleConnection("OracleConnection");
                command = new OracleCommand(procedureName, oracleConnection)
                {
                    CommandType = CommandType.StoredProcedure
                };

                // Thêm các tham số vào command
                foreach (var param in parameters)
                {
                    var oracleParameter = new OracleParameter
                    {
                        Direction = (ParameterDirection)param[0],
                        OracleDbType = (OracleDbType)param[1],
                        ParameterName = (string)param[2],
                        Value = param[3] ?? DBNull.Value
                    };

                    // Kiểm tra nếu tham số có kích thước (phần tử thứ 5) và thiết lập kích thước nếu có
                    if (param.Length > 4 && param[4] != null && param[4] is int size)
                    {
                        oracleParameter.Size = size;
                    }

                    command.Parameters.Add(oracleParameter);
                }

                // Sử dụng OracleDataAdapter để điền dữ liệu vào DataSet
                adapter = new OracleDataAdapter(command);
                adapter.Fill(dataSet);
            }
            catch (Exception ex)
            {
                // Ghi log hoặc xử lý ngoại lệ tại đây nếu cần, sau đó ném lại ngoại lệ
                throw new Exception("Error: " + ex.Message, ex);
            }
            finally
            {
                // Đóng và giải phóng tài nguyên
                if (adapter != null) adapter.Dispose();
                if (command != null) command.Dispose();
                if (oracleConnection != null)
                {
                    oracleConnection.Close();
                    oracleConnection.Dispose();
                }
            }

            return dataSet;
        }

        public static Dictionary<string, object> ExecuteStoredProcedureOracle1(string procedureName, object[][] parameters)
        {
            OracleConnection oracleConnection = null;
            OracleCommand command = null;
            Dictionary<string, object> outputValues = new Dictionary<string, object>();

            try
            {
                oracleConnection = OpenOracleConnection("OracleConnection");
                command = new OracleCommand(procedureName, oracleConnection)
                {
                    CommandType = CommandType.StoredProcedure
                };

                // Thêm các tham số vào command
                foreach (var param in parameters)
                {
                    var oracleParameter = new OracleParameter
                    {
                        Direction = (ParameterDirection)param[0],
                        OracleDbType = (OracleDbType)param[1],
                        ParameterName = (string)param[2],
                        Value = param[3] ?? DBNull.Value
                    };

                    // Kiểm tra nếu tham số có kích thước (phần tử thứ 5) và thiết lập kích thước nếu có
                    if (param.Length > 4 && param[4] != null && param[4] is int size)
                    {
                        oracleParameter.Size = size;
                    }

                    command.Parameters.Add(oracleParameter);
                }

                // Thực thi stored procedure
                command.ExecuteNonQuery();

                // Lấy các giá trị của tham số Output và lưu vào outputValues
                foreach (OracleParameter param in command.Parameters)
                {
                    if (param.Direction == ParameterDirection.Output || param.Direction == ParameterDirection.InputOutput)
                    {
                        outputValues[param.ParameterName] = param.Value;
                    }
                }
            }
            catch (Exception ex)
            {
                // Ghi log hoặc xử lý ngoại lệ tại đây nếu cần, sau đó ném lại ngoại lệ
                throw new Exception("Error: " + ex.Message, ex);
            }
            finally
            {
                // Đóng và giải phóng tài nguyên
                if (command != null) command.Dispose();
                if (oracleConnection != null)
                {
                    oracleConnection.Close();
                    oracleConnection.Dispose();
                }
            }

            return outputValues;
        }


        /////////////////////////////////////////////////////////////////

        /// <summary>
        /// Thực thi câu lệnh SQL trực tiếp trên SQL Server và trả về kết quả dưới dạng DataSet.
        /// </summary>
        /// <param name="sSQL">Câu lệnh SQL cần thực thi</param>
        /// <returns>DataSet chứa dữ liệu trả về từ câu lệnh SQL</returns>
        public static DataSet ExecuteSQLSqlServer(string sSQL)
        {
            return ExecuteSQLSqlServer(sSQL, null);
        }

        /// <summary>
        /// Thực thi câu lệnh SQL trực tiếp trên SQL Server với các tham số và trả về kết quả dưới dạng DataSet.
        /// </summary>
        /// <param name="sSQL">Câu lệnh SQL cần thực thi</param>
        /// <param name="Params">Mảng các tham số</param>
        /// <returns>DataSet chứa dữ liệu trả về từ câu lệnh SQL</returns>
        public static DataSet ExecuteSQLSqlServer(string sSQL, object[][] Params)
        {
            SqlConnection sqlConnection = null;
            SqlCommand command = null;
            SqlDataAdapter adapter = null;
            DataSet dataSet = new DataSet();

            try
            {
                sqlConnection = OpenSqlConnection();
                command = new SqlCommand(sSQL, sqlConnection);

                // Thêm các tham số vào command nếu có
                if (Params != null)
                {
                    foreach (var param in Params)
                    {
                        var sqlParameter = new SqlParameter
                        {
                            Direction = (ParameterDirection)param[0],
                            SqlDbType = (SqlDbType)param[1],
                            ParameterName = (string)param[2],
                            Value = param[3] ?? DBNull.Value
                        };
                        command.Parameters.Add(sqlParameter);
                    }
                }

                adapter = new SqlDataAdapter(command);
                adapter.Fill(dataSet);
                return dataSet;
            }
            catch (Exception ex)
            {
                // Ghi log hoặc xử lý ngoại lệ tại đây nếu cần, sau đó ném lại ngoại lệ
                throw new Exception("Error: " + ex.Message, ex);
            }
            finally
            {
                if (adapter != null) adapter.Dispose();
                if (command != null) command.Dispose();
                if (sqlConnection != null)
                {
                    sqlConnection.Close();
                    sqlConnection.Dispose();
                }
            }
        }

        /// <summary>
        /// Thực thi câu lệnh SQL trực tiếp trên Oracle Database và trả về kết quả dưới dạng DataSet.
        /// </summary>
        /// <param name="sSQL">Câu lệnh SQL cần thực thi</param>
        /// <returns>DataSet chứa dữ liệu trả về từ câu lệnh SQL</returns>
        public static DataSet ExecuteSQLOracle(string sSQL)
        {
            return ExecuteSQLOracle(sSQL, null);
        }

        /// <summary>
        /// Thực thi câu lệnh SQL trực tiếp trên Oracle Database với các tham số và trả về kết quả dưới dạng DataSet.
        /// </summary>
        /// <param name="sSQL">Câu lệnh SQL cần thực thi</param>
        /// <param name="Params">Mảng các tham số</param>
        /// <returns>DataSet chứa dữ liệu trả về từ câu lệnh SQL</returns>
        public static DataSet ExecuteSQLOracle(string sSQL, object[][] Params)
        {
            OracleConnection oracleConnection = null;
            OracleCommand command = null;
            OracleDataAdapter adapter = null;
            DataSet dataSet = new DataSet();

            try
            {
                oracleConnection = OpenOracleConnection("OracleConnection");
                command = new OracleCommand(sSQL, oracleConnection);

                // Thêm các tham số vào command nếu có
                if (Params != null)
                {
                    foreach (var param in Params)
                    {
                        var oracleParameter = new OracleParameter
                        {
                            Direction = (ParameterDirection)param[0],
                            OracleDbType = (OracleDbType)param[1],
                            ParameterName = (string)param[2],
                            Value = param[3] ?? DBNull.Value
                        };
                        command.Parameters.Add(oracleParameter);
                    }
                }

                adapter = new OracleDataAdapter(command);
                adapter.Fill(dataSet);
                return dataSet;
            }
            catch (Exception ex)
            {
                // Ghi log hoặc xử lý ngoại lệ tại đây nếu cần, sau đó ném lại ngoại lệ
                throw new Exception("Error: " + ex.Message, ex);
            }
            finally
            {
                if (adapter != null) adapter.Dispose();
                if (command != null) command.Dispose();
                if (oracleConnection != null)
                {
                    oracleConnection.Close();
                    oracleConnection.Dispose();
                }
            }
        }


        // Log lỗi (cần tự định nghĩa phương thức addConnLog nếu cần)
        private static void addConnLog(LogType logType, string message, string errorDetail)
        {
            // Implement logging mechanism here
            Console.WriteLine($"{logType}: {message} - {errorDetail}");
        }

    }
}

