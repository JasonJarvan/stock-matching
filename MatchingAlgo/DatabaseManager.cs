using System;
using System.Data;
using System.Data.SqlClient;
using MatchingAlgo;

internal static class DatabaseManager
{
    private static readonly string connectionString = "Server=localhost;Database=matching;User Id=postgres;Password=1996;";

    public static void CreateTableOrder()
    {
        string query = @"
            CREATE TABLE dbo.torder (
                OrderID INT PRIMARY KEY,
                Ticker VARCHAR(10),
                OrderDate INT,
                OrderTime VARCHAR(15),
                OrderQty INT,
                OrderPrc FLOAT,
                FillQty INT,
                FillMoneyAmount FLOAT
            );
        ";

        ExecuteNonQuery(query);
    }

    public static void CreateTableFill()
    {
        string query = @"
            CREATE TABLE dbo.tfill (
                OrderID INT,
                FillID INT PRIMARY KEY,
                FillTime VARCHAR(15),
                FillQty INT,
                FillPrice FLOAT,
                FillMoneyAmount FLOAT
            );
        ";

        ExecuteNonQuery(query);
    }

    public static void UpdateOrder(int orderID, int fillQty, double fillMoneyAmount)
    {
        string query = @"
        UPDATE dbo.torder
        SET FillQty = FillQty + @FillQty,
            FillMoneyAmount = FillMoneyAmount + @FillMoneyAmount
        WHERE OrderID = @OrderID;
    ";

        ExecuteNonQuery(query, new { OrderID = orderID, FillQty = fillQty, FillMoneyAmount = fillMoneyAmount });
    }

    public static void InsertOrder(OrderData order)
    {
        string query = @"
            INSERT INTO dbo.torder (OrderID, Ticker, OrderDate, OrderTime, OrderQty, OrderPrc, FillQty, FillMoneyAmount)
            VALUES (@OrderID, @Ticker, @OrderDate, @OrderTime, @OrderQty, @OrderPrc, @FillQty, @FillMoneyAmount);
        ";

        ExecuteNonQuery(query, order);
    }

    public static void InsertFill(FillData fill)
    {
        string query = @"
            INSERT INTO dbo.tfill (OrderID, FillID, FillTime, FillQty, FillPrice, FillMoneyAmount)
            VALUES (@OrderID, @FillID, @FillTime, @FillQty, @FillPrice, @FillMoneyAmount);
        ";

        ExecuteNonQuery(query, fill);
    }

    static void ExecuteNonQuery(string query, object parameters = null)
    {
        using (SqlConnection connection = new SqlConnection(connectionString))
        {
            using (SqlCommand command = new SqlCommand(query, connection))
            {
                if (parameters != null)
                {
                    foreach (var property in parameters.GetType().GetProperties())
                    {
                        command.Parameters.AddWithValue("@" + property.Name, property.GetValue(parameters));
                    }
                }

                connection.Open();
                command.ExecuteNonQuery();
            }
        }
    }
}
