using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.SqlClient;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace SQL_Helper
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            Clipboard.SetText("Server=.\\SQLExpress;Database=iris;Trusted_Connection=True;");
            InitializeComponent();
        }

        private string code = "";
        private Dictionary<string, string> colunas = new Dictionary<string, string>();

        private void checkBox1_CheckedChanged(object sender, EventArgs e)
        {
            tbConn.Text = !checkBox1.Checked
                ? "Server=myServerAddress;Database=myDataBase;User Id=myUsername;Password = myPassword;"
                : "Server=myServerAddress;Database=myDataBase;Trusted_Connection=True;";
        }

        private void button2_Click(object sender, EventArgs e)
        {
            checkedListBox1.Items.Clear();
            colunas.Clear();
            string text = tbConn.Text;
            StringBuilder stringBuilder = new StringBuilder();
            if (!string.IsNullOrEmpty(text))
            {
                using (SqlConnection connection = new SqlConnection(text))
                {
                    connection.Open();
                    using (SqlCommand sqlCommand = new SqlCommand("sp_columns '" + tbTable.Text + "'", connection))
                    {
                        using (SqlDataReader sqlDataReader = sqlCommand.ExecuteReader())
                        {
                            while (sqlDataReader.Read())
                            {
                                string str = sqlDataReader["COLUMN_NAME"].ToString();
                                string lower = sqlDataReader["IS_NULLABLE"].ToString().ToLower();
                                string type1 = GetType(sqlDataReader["TYPE_NAME"].ToString());
                                string type2 = type1 + (!(lower == "yes") || !(type1 != "string") ? "" : "?");
                                stringBuilder.AppendLine(FetchField(str, type2));
                                colunas.Add(str, type2);
                            }
                        }
                    }
                    connection.Close();
                }
            }
            this.checkedListBox1.Items.AddRange((object[])this.colunas.Keys.ToArray<string>());
            this.code = stringBuilder.ToString();
        }

        private string FetchField(string column, string type)
        {
            return (!this.radioButton1.Checked
                ?
                "\t\tprivate [type] _[column];" +
                "\n\n" +
                "\t\tpublic [type] [column]\n" +
                "\t\t{\n" +
                "\t\t\tget { return _[column]; }\n" +
                "\t\t\tset { _[column] = value; }\n" +
                "\t\t}"
                :
                "\t\tpublic [type] [column] { get; set; }")
                .Replace("[type]", type)
                .Replace("[column]", column)
                + "\n\n";
        }

        public string GetType(string type)
        {
            type = type.Replace("identity", "").Trim();
            string lower = type.ToLower();
            if (lower != null)
            {
                switch (lower)
                {
                    case "bigint":
                        return "long";
                    case "binary":
                    case "image":
                    case "timestamp":
                    case "varbinary":
                        return "byte[]";
                    case "bit":
                        return "bool";
                    case "char":
                    case "nchar":
                    case "ntext":
                    case "nvarchar":
                    case "text":
                    case "varchar":
                    case "xml":
                        return "string";
                    case "date":
                    case "datetime":
                    case "datetime2":
                    case "smalldatetime":
                    case "time":
                        return "DateTime";
                    case "datetimeoffset":
                        return "DateTimeOffset";
                    case "decimal":
                    case "money":
                    case "smallmoney":
                        return "decimal";
                    case "float":
                        return "double";
                    case "int":
                        return "int";
                    case "real":
                        return "float";
                    case "smallint":
                        return "short";
                    case "tinyint":
                        return "short";
                    case "uniqueidentifier":
                        return "Guid";
                }
            }
            return "";
        }

        private void button1_Click(object sender, EventArgs e)
        {
            List<string> list = this.checkedListBox1.CheckedItems.Cast<string>().ToList<string>();
            StringBuilder stringBuilder = new StringBuilder();
            List<string> stringList = new List<string>();
            stringBuilder.AppendLine("\t\tpublic [class][header]");
            stringBuilder.AppendLine("\t\t{");
            foreach (string key in list)
            {
                if (this.colunas.ContainsKey(key))
                {
                    string valueOrDefault = colunas[key];
                    stringBuilder.AppendLine("\t\t\tthis." + key + " = " + key + ";");
                    stringList.Add(valueOrDefault + " " + key);
                }
            }
            stringBuilder.AppendLine("\t\t}");
            stringBuilder.AppendLine(Environment.NewLine);
            string newValue = stringBuilder.ToString().Replace("[class]", tbClass.Text).Replace("[header]", "(" + string.Join(", ", (IEnumerable<string>)stringList) + ")");
            string str = "";
            if (File.Exists(Path.Combine(Application.StartupPath, "BaseClass.txt")))
                str = File.ReadAllText(Path.Combine(Application.StartupPath, "BaseClass.txt"));
            new Form2(str.Replace("[code]", code).Replace("[class]", tbClass.Text).Replace("[constructor]", newValue).Replace("[namespace]", this.tbNamespace.Text)).Show();
        }
    }
}
