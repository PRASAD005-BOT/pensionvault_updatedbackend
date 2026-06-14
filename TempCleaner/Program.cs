using System;
using Npgsql;

class Program
{
    static void Main()
    {
        string connStr = "Host=dpg-d8mj067lk1mc738oe5lg-a.oregon-postgres.render.com;Database=pensionvault_db;Username=pensionvault_db_user;Password=e465vmR9nGn5ZJEjqNdYDsZgN9tv8sKF;SslMode=Require;";
        using var conn = new NpgsqlConnection(connStr);
        conn.Open();
        using var cmd = new NpgsqlCommand("DELETE FROM \"InvestmentPortfolios\"; DELETE FROM \"CorpusRecords\";", conn);
        int rows = cmd.ExecuteNonQuery();
        Console.WriteLine($"Mock data cleared. Rows affected: {rows}");
    }
}
