using System.Data;
namespace DatenbankManagementSystem {
    public class Table {
        public DataTable table { get; set; }
        public string tableName { get; set; }
        public Table(DataTable table, string tableName) {
            this.table = table;
            this.tableName = tableName;
        }
    }
}
