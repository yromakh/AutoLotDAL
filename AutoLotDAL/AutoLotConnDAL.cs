using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data;
using System.Data.SqlClient;

namespace AutoLotConnectedLayer
{
    public class Inventory1DAL
    {
        private SqlConnection sqlCn = null;

        public void OpenConnection(string connectionString)
        {
            sqlCn = new SqlConnection();
            sqlCn.ConnectionString = connectionString;
            sqlCn.Open();
        }

        public void CloseConnection()
        {
            sqlCn.Close();
        }     

        public void InsertAuto(NewCar car)
        {
            string sql = string.Format("INSERT INTO Inventory1 (CarID, Make, Color, PetName)" +
                                       "VALUES ('{0}', '{1}', '{2}', {3})", car.CarID, car.Make, car.Color, car.PetName);

            using(SqlCommand cmd = new SqlCommand(sql, this.sqlCn))
            {
                cmd.ExecuteNonQuery();
            }
        }

        public void InsertAuto(int id, string color, string make, string petName)
        {
            string sql = string.Format("INSERT INTO Inventory1 (CarID, Make, Color, PetName)" +
                "VALUES (@CarID, @Make, @Color, @PetName)");

            using(SqlCommand cmd = new SqlCommand(sql, this.sqlCn))
            {
                SqlParameter param = new SqlParameter();
                param.ParameterName = "@CarID";
                param.Value = id;
                param.SqlDbType = SqlDbType.Int; 
                cmd.Parameters.Add(param);

                param = new SqlParameter();
                param.ParameterName = "@Make";
                param.Value = make;
                param.SqlDbType = SqlDbType.Char;
                param.Size = 10;
                cmd.Parameters.Add(param);

                param = new SqlParameter();
                param.ParameterName = "@Color";
                param.Value = color;
                param.SqlDbType = SqlDbType.Char;
                param.Size = 10;
                cmd.Parameters.Add(param);

                param = new SqlParameter();
                param.ParameterName = "@PetName";
                param.Value = petName;
                param.SqlDbType = SqlDbType.Char;
                param.Size = 10;
                cmd.Parameters.Add(param);

                cmd.ExecuteNonQuery();
            }
        }

        public void DeleteCar(int id)
        {
            string sql = string.Format("DELETE FROM Inventory1 WHERE CarID = '{0}'", id);

            using (SqlCommand cmd = new SqlCommand(sql, this.sqlCn))
            {
                try
                {
                    cmd.ExecuteNonQuery();
                }
                catch (SqlException ex)
                {
                    Exception error = new Exception("Sorry! That car is on order!", ex);
                    throw error;
                }
            }
        }

        public void UpdateCarPetName(int id, string newPetName)
        {
            string sql = string.Format("UPDATE Inventory1 SET PetName = '{0}' WHERE CarID = '{1}'", newPetName, id);

            using(SqlCommand cmd = new SqlCommand(sql, this.sqlCn))
            {
                cmd.ExecuteNonQuery();
            }
        }

        public List<NewCar> GetAllInventory1AsList()
        {
            List<NewCar> inv = new List<NewCar>();

            string sql = "SELECT * FROM Inventory1";

            using(SqlCommand cmd = new SqlCommand(sql, this.sqlCn))
            {
                SqlDataReader dr = cmd.ExecuteReader();

                while(dr.Read())
                {
                    inv.Add(new NewCar
                        {
                            CarID = (int)dr["CarID"],
                            Color = (string)dr["Color"],
                            Make = (string)dr["Make"],
                            PetName = (string)dr["PetName"]
                        });
                }
                dr.Close();
            }
            return inv;
        }

        public DataTable GetAllInventory1AsDataTable()
        {
            DataTable inv = new DataTable();

            string sql = "SELECT * FROM Inventory1";
            using(SqlCommand cmd = new SqlCommand(sql, this.sqlCn))
            {
                SqlDataReader dr = cmd.ExecuteReader();

                // fill in DataTable with data and repform cleaning
                inv.Load(dr);
                dr.Close();
            }

            return inv;
        }

        public string LookUpPetName(int carID)
        {
            string carPetName = string.Empty;

            //set name of storage procedure
            using(SqlCommand cmd = new SqlCommand("GetPetName", this.sqlCn ))
            {
                cmd.CommandType = CommandType.StoredProcedure;

                // input param
                SqlParameter param = new SqlParameter();
                param.ParameterName = "@carID";
                param.SqlDbType = SqlDbType.Int;
                param.Value = carID;
                param.Direction = ParameterDirection.Input;
                cmd.Parameters.Add(param);

                // output param
                param = new SqlParameter();
                param.ParameterName = "@petName";
                param.SqlDbType = SqlDbType.Char;
                param.Size = 10;
                param.Direction = ParameterDirection.Output;
                cmd.Parameters.Add(param);

                cmd.ExecuteNonQuery();

                // call output param
                carPetName = (string)cmd.Parameters["@petName"].Value;
            }
            return carPetName;
        }

        public void processCreditRisk(bool throwEx, int custID)
        {
            string fName = string.Empty;
            string lName = string.Empty;

            SqlCommand cmdSelect = new SqlCommand(string.Format(
                "SELECT * FROM Customers WHERE CustID = {0}", custID), sqlCn);

            using (SqlDataReader dr = cmdSelect.ExecuteReader())
            {
                if (dr.HasRows)
                {
                    dr.Read();
                    fName = (string)dr["FirstName"];
                    lName = (string)dr["LastName"];
                }
                else
                    return;
            }

            SqlCommand cmdRemove = new SqlCommand(string.Format(
                "DELETE FROM Customers WHERE CustID = {0}", custID), sqlCn);

            SqlCommand cmdInsert = new SqlCommand(string.Format(
                "INSERT INTO CreditRisks (CustID, FirstName, LastName) VALUES ({0}, '{1}', '{2}')", custID, fName, lName), sqlCn);

            SqlTransaction tx = null;
            try
            {
                tx = sqlCn.BeginTransaction();

                cmdInsert.Transaction = tx;
                cmdRemove.Transaction = tx;

                cmdInsert.ExecuteNonQuery();
                cmdRemove.ExecuteNonQuery();

                if(throwEx)
                {
                    throw new ApplicationException("Database error! Transaction finished unsuccessfully");
                }

                tx.Commit();
            }
            catch(Exception ex)
            {
                Console.WriteLine(ex.Message);
                tx.Rollback();
            }
        }
    }

    public class NewCar
    {
        public int CarID { get; set; }
        public string Color { get; set; }
        public string Make { get; set; }
        public string PetName { get; set; }
    }
}