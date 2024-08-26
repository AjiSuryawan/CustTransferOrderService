﻿using CustTransferOrderService.models;
using Newtonsoft.Json;
using Quartz;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CustTransferOrderService
{
    internal class MyJob : IJob
    {
        public void MainExecute()
        {
            string csvFilePath = ConfigurationManager.AppSettings["csvFilePath"];
            string csvFilePathError = ConfigurationManager.AppSettings["csvFilePathError"];
            string tableName = ConfigurationManager.AppSettings["TableName"];
            string jsonFilePath = ConfigurationManager.AppSettings["jsonFilePath"];

            // Load and deserialize JSON format specifications
            string jsonString = File.ReadAllText(jsonFilePath);
            FormatConfig formatConfig = JsonConvert.DeserializeObject<FormatConfig>(jsonString);

            string DBServer = ConfigurationManager.AppSettings["DBServer"];
            string DBName = ConfigurationManager.AppSettings["DBName"];
            string DBUser = ConfigurationManager.AppSettings["DBUser"];
            string DBPass = ConfigurationManager.AppSettings["DBPass"];
            string Timeout = ConfigurationManager.AppSettings["Timeout"];

            string connectionString = @"Server=" + DBServer + ";Database=" + DBName + ";Uid=" + DBUser + ";Pwd=" + DBPass + ";Encrypt=True;TrustServerCertificate=True;Connection Timeout=" + Timeout + ";Integrated Security=True";
            string query = "SELECT * FROM " + tableName;

            List<TransferModel> transfers = new List<TransferModel>();

            try
            {
                SqlConnection conn = new SqlConnection(connectionString);
                SqlCommand cmd = new SqlCommand(query, conn);
                conn.Open();
                SqlDataReader reader = cmd.ExecuteReader();

                while (reader.Read())
                {
                    TransferModel order = new TransferModel
                    {
                        ItemCode = reader["ItemCode"].ToString().Trim(),
                        SourceLocation = reader["SourceLocation"].ToString().Trim(),
                        DestinationLocation = reader["DestinationLocation"].ToString().Trim(),
                        TransferNumber = reader["TransferNumber"].ToString().Trim(),
                        LineNumber = reader["LineNumber"].ToString().Trim(),
                        TransferDate = reader.GetDateTime(reader.GetOrdinal("TransferDate")),
                        ExpectedArrivalDate = reader.GetDateTime(reader.GetOrdinal("ExpectedArrivalDate")),
                        TransferQty = Convert.ToDecimal(reader["TransferQty"]),
                        QuantityToShip = Convert.ToDecimal(reader["QuantityToShip"]),
                        QuantityToReceive = Convert.ToDecimal(reader["QuantityToReceive"])
                    };
                    transfers.Add(order);
                }

                // Close resources
                reader.Close();
                cmd.Dispose();
                conn.Close();
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }

            // Write data to CSV file with format specifications from JSON
            StreamWriter writer = new StreamWriter(csvFilePath);
            StreamWriter writerFailed = new StreamWriter(csvFilePathError);


            foreach (var order in transfers)
            {
                var values = new List<string>();
                var valuesFailed = new List<string>();
                try
                {


                    foreach (var property in typeof(TransferModel).GetProperties())
                    {
                        string columnName = property.Name;
                        var formatSpec = formatConfig.Formats.Find(f => f.FieldName == columnName);
                        var value = property.GetValue(order);

                        if (formatSpec != null)
                        {
                            if (formatSpec.DataType.Equals("String"))
                            {
                                if (formatSpec.Length > 0)
                                {
                                    int totalLength = formatSpec.Length;
                                    string valueString = value.ToString();
                                    string textWithSpace = valueString.PadLeft(totalLength);
                                    values.Add(textWithSpace);
                                }
                                else
                                {
                                    values.Add(value.ToString());
                                }

                            }
                            else if (formatSpec.DataType.Equals("Date"))
                            {
                                if (formatSpec.DefaultValue.Equals(""))
                                {
                                    try
                                    {
                                        DateTime dateValue = (DateTime)value;
                                        values.Add(dateValue.ToString(formatSpec.Format));
                                    }
                                    catch (FormatException)
                                    {
                                        Console.WriteLine("Parsing failed for column '" + columnName + "'. Original value added: " + value);
                                        //values.Add(value.ToString());
                                        values.Add("19000000");
                                    }
                                }
                                else
                                {
                                    values.Add(formatSpec.DefaultValue.ToString());
                                }


                            }
                            else if (formatSpec.DataType.Equals("Numeric"))
                            {
                                if (formatSpec.DefaultValue.Equals(""))
                                {
                                    // Adjust formatting for numeric types
                                    try
                                    {
                                        decimal numericValue;
                                        if (decimal.TryParse(value.ToString(), out numericValue))
                                        {
                                            values.Add(numericValue.ToString(formatSpec.Format)); // Use Format from JSON
                                        }
                                        else
                                        {
                                            values.Add(value.ToString());
                                        }
                                    }
                                    catch (Exception e)
                                    {
                                        Console.WriteLine(e.ToString());
                                    }
                                }
                                else
                                {
                                    values.Add(formatSpec.DefaultValue.ToString());
                                }

                            }
                            else
                            {
                                values.Add(value.ToString());
                            }
                        }
                        else
                        {
                            values.Add(value.ToString());
                        }
                    }

                    string line = string.Join(";", values);
                    writer.WriteLine(line);
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.ToString());
                    valuesFailed.Add(values.ToString());
                    string line = string.Join(";", valuesFailed);
                    writerFailed.WriteLine(line);
                }

            }
            writer.Close();
            writerFailed.Close();

        }

        public async System.Threading.Tasks.Task Execute(IJobExecutionContext context)
        {
            MainExecute();
        }
    }
}
