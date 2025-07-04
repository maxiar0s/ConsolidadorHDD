using Npgsql;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Documents;

namespace ConsolidadorHDD
{
    public class DataBase
    {
        public static string connectionString = "Host=144.22.41.245;Username=siroe;Password=siroe;Database=DBNasFiles";

        public static async void getProcess() {
            Debug.WriteLine("empecemos");
            await using var dataSource = NpgsqlDataSource.Create(connectionString);

            await using (var cmd = dataSource.CreateCommand("SELECT state, directory_process FROM process"))
            await using (var reader = await cmd.ExecuteReaderAsync())
            {
                while (await reader.ReadAsync())
                {
                    Debug.WriteLine(reader.GetString(0) + ", " + reader.GetString(1));
                }
            }
        }

        public static async Task<List<Storage>> getAllStorages() {
            Debug.WriteLine("Get Storages");
            var storages = new List<Storage>();

            await using var dataSource = NpgsqlDataSource.Create(connectionString);

            await using (var cmd = dataSource.CreateCommand("SELECT id, name, external_id FROM storage"))
            await using (var reader = await cmd.ExecuteReaderAsync())
            {
                while (await reader.ReadAsync())
                {
                    Debug.WriteLine(reader.GetString(0) + ", " + reader.GetString(1) + "," + reader.GetString(2));
                    storages.Add(new Storage
                    {
                        ID = reader.GetString(0),
                        name = reader.GetString(1),
                        external_id = reader.GetString(2)
                    });
                }
            }

            return storages;
        }

        public static async Task<List<FileData>> getHashNas() {
            Debug.WriteLine("Get Storages");
            List<FileData> Nasdata = new List<FileData>();


            await using var dataSource = NpgsqlDataSource.Create(connectionString);

            await using (var cmd = dataSource.CreateCommand($"SELECT id, path, file_name, checksum FROM files;"))
            await using (var reader = await cmd.ExecuteReaderAsync())
            {
                while (await reader.ReadAsync())
                {
                    //Debug.WriteLine(reader.GetString(1) + "," + reader.GetString(2));
                    Nasdata.Add(new FileData
                    {
                        id = reader.GetInt32(0),
                        Directorios = reader.GetString(1),
                        Nombre = reader.GetString(2),
                        Hash = reader.GetString(3)

                    });
                }
            }

            return Nasdata;
        }

    }
}
