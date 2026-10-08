using Microsoft.AspNetCore.Mvc;
using System.Data;
using MySql.Data.MySqlClient;
using Horizon.Repository.ECW;

namespace prepchartApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
     public class HomeController : ControllerBase
     {

        private readonly IConfiguration _config;

        public HomeController(IConfiguration config)
        {
            _config = config;
        }

        [HttpGet("daily")]
        public IActionResult GetDailyVisitInfo(string date, string provider, string facility)
        {
            try
            {
                var results = new List<object>();
                string connStr = _config.GetConnectionString("LiveHuddleDb");
                using (var conn = new MySqlConnection(connStr))
                {
                    conn.Open();
                    string query = "CALL getDailyVisitInfo(@date, @provider, @facility)";
                    using (var cmd = new MySqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@date", date);
                        cmd.Parameters.AddWithValue("@provider", provider);
                        cmd.Parameters.AddWithValue("@facility", facility);
                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                results.Add(new
                                {
                                    encounterid = reader["encounterid"],
                                    Patient = reader["patient"],
                                    controllNo = reader["ptaccount"],
                                    Preppedby = reader["preppedby"],
                                    Provider = reader["providername"],
                                    Facility = reader["facilityname"],
                                    visitdate = reader["visitdate"],
                                    preppedtime = reader["preppedtime"],
                                    starttime = reader["starttime"],
                                    visittype = reader["visittype"],
                                    pendingflag = reader["pendingflag"]
                                });
                            }
                        }
                    }
                }
                return Ok(results);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    Success = false,
                    Message = ex.Message,
                    Inner = ex.InnerException?.Message,
                    Full = ex.ToString()
                });
            }
        }

        //providers and the facilites

        [HttpGet("providersForDate")]
        public IActionResult GetProvidersForDate(string date, string? facility)
        {
            string connStr = _config.GetConnectionString("LiveHuddleDb");
            var providers = new List<string>();

            try
            {
                using (var conn = new MySqlConnection(connStr))
                {
                    conn.Open();
                    string query = "CALL getDailyVisitInfo(@date, @provider, @facility)";
                    using (var cmd = new MySqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@date", date);
                        cmd.Parameters.AddWithValue("@provider", "%%"); // wildcard — we want every provider
                        cmd.Parameters.AddWithValue("@facility", string.IsNullOrWhiteSpace(facility) ? "%%" : facility);

                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                var name = reader["providername"]?.ToString();
                                if (!string.IsNullOrWhiteSpace(name) && !providers.Contains(name))
                                    providers.Add(name);
                            }
                        }
                    }
                }
                providers.Sort(StringComparer.OrdinalIgnoreCase);
                return Ok(providers);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Success = false, Message = ex.Message });
            }
        }

        [HttpGet("facilitiesForDate")]
        public IActionResult GetFacilitiesForDate(string date, string? provider)
        {
            string connStr = _config.GetConnectionString("LiveHuddleDb");
            var facilities = new List<string>();

            try
            {
                using (var conn = new MySqlConnection(connStr))
                {
                    conn.Open();
                    string query = "CALL getDailyVisitInfo(@date, @provider, @facility)";
                    using (var cmd = new MySqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@date", date);
                        cmd.Parameters.AddWithValue("@provider", string.IsNullOrWhiteSpace(provider) ? "%%" : provider);
                        cmd.Parameters.AddWithValue("@facility", "%%"); // wildcard — we want every facility

                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                var name = reader["facilityname"]?.ToString();
                                if (!string.IsNullOrWhiteSpace(name) && !facilities.Contains(name))
                                    facilities.Add(name);
                            }
                        }
                    }
                }
                facilities.Sort(StringComparer.OrdinalIgnoreCase);
                return Ok(facilities);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Success = false, Message = ex.Message });
            }
        }

        [HttpGet("markPrepped")]
        public IActionResult PreppedOrNot(int encounterid, string username)
        {
            string connStr = _config.GetConnectionString("LiveHuddleDb");

            using (var conn = new MySqlConnection(connStr))
            {
                conn.Open();
                string q  = "CALL markPrepped(@encounterid, @username)";
                using (var cmd = new MySqlCommand(q, conn))
                {
                    cmd.Parameters.AddWithValue("@encounterid", encounterid);
                    cmd.Parameters.AddWithValue("@username", username);
                    int rowsAffected = cmd.ExecuteNonQuery();
                    return Ok(new { Success = true, RowsAffected = rowsAffected });
                }
            }
        }

        [HttpGet("markNotPrepped")]
        public IActionResult PreppedOrNot(int encounterid)
        {
            string connStr = _config.GetConnectionString("LiveHuddleDb");

            using (var conn = new MySqlConnection(connStr))
            {
                conn.Open();
                string q  = "CALL markNotPrepped(@encounterid)";
                using (var cmd = new MySqlCommand(q, conn))
                {
                    cmd.Parameters.AddWithValue("@encounterid", encounterid);
                    int rowsAffected = cmd.ExecuteNonQuery();
                    return Ok(new { Success = true, RowsAffected = rowsAffected });
                }
            }
        }

        [HttpGet("markPendingOrNot")]
        public IActionResult markNotPending(int encounterid , int pending )
        {
             string connStr = _config.GetConnectionString("LiveHuddleDb");

            using (var conn = new MySqlConnection(connStr))
            {
                conn.Open();
                string q  = "CALL setPendingStatusForChartPrep(@encounterid , @pending)";
                using (var cmd = new MySqlCommand(q, conn))
                {
                    cmd.Parameters.AddWithValue("@encounterid", encounterid);
                    cmd.Parameters.AddWithValue("@pending", pending);
                    int rowsAffected = cmd.ExecuteNonQuery();
                    return Ok(new { Success = true, RowsAffected = rowsAffected });
                }
            }
        }
       
       [HttpGet("pendingordersLH")]
        public IActionResult PendingOrdersLH()
        {
            string connStr = _config.GetConnectionString("LiveHuddleDb");

            try
            {
                using (var conn = new MySqlConnection(connStr))
                {
                    conn.Open();

                    string q = @"
                        SELECT
                            v.encounterid,
                            p.ptaccount,
                            p.ptname,
                            v.pendingordersflag
                        FROM visits v
                        INNER JOIN patients p ON v.patientID = p.patientID
                        WHERE v.pendingordersflag = 1;
                    ";

                    using (var cmd = new MySqlCommand(q, conn))
                    using (var reader = cmd.ExecuteReader())
                    {
                        var result = new List<object>();

                        while (reader.Read())
                        {
                            result.Add(new
                            {
                                EncounterID = reader["encounterid"],
                                PtAccount = reader["ptaccount"],
                                PtName = reader["ptname"],
                                PendingOrdersFlag = reader["pendingordersflag"]
                            });
                        }

                        return Ok(new { Success = true, Count = result.Count, Data = result });
                    }
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Success = false, Message = ex.Message });
            }
        }

        [HttpGet("preppedByByFacility")]
        public IActionResult PreppedByByFacility([FromQuery] string date)
        {
            string connStr = _config.GetConnectionString("LiveHuddleDb");

            try
            {
                using (var conn = new MySqlConnection(connStr))
                {
                    conn.Open();

                    string q1 = @"
                        SELECT
                            f.facilityname,
                            v.preppedby
                        FROM visits v 
                        LEFT JOIN facilities f ON v.facility = f.facilityid
                        WHERE v.visitdate = @date
                        AND v.preppedby IS NOT NULL
                        GROUP BY f.facilityname
                        ORDER BY COUNT(*) DESC;
                    ";

                    List<string> facilities = new List<string>();

                    using (var cmd1 = new MySqlCommand(q1, conn))
                    {
                        cmd1.Parameters.AddWithValue("@date", date);

                        using (var reader = cmd1.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                facilities.Add(reader["facilityname"].ToString());
                            }
                        }
                    }

                    if (facilities.Count == 0)
                    {
                        return Ok(new { Success = true, Count = 0, Data = new List<object>() });
                    }

                    string q2 = $@"
                        SELECT
                            p.ptaccount,
                            p.ptname,
                            v.encounterid,
                            v.visitdate,
                            v.preppedby,
                            v.preppedtime,
                            v.visittype
                        FROM visits v
                        LEFT JOIN facilities f ON v.facility = f.facilityid
                        LEFT JOIN patients p ON v.patientID = p.patientID
                        WHERE v.visitdate = @date
                        AND f.facilityname IN ({string.Join(",", facilities.Select(f => $"'{f}'"))})
                        AND v.preppedby IS NULL
                        AND v.delflag = 0
                        AND v.status NOT IN ('CANC','N/S','R/S','CANCPHONE');
                    ";

                    var patients = new List<object>();
                    using (var cmd2 = new MySqlCommand(q2, conn))
                    {
                        cmd2.Parameters.AddWithValue("@date", date);

                        using (var reader = cmd2.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                string visitType = reader["visittype"].ToString();
                                if (visitType == "NEW" || visitType == "NP" || visitType == "NPT" || visitType == "NPT1")
                                    continue;

                                patients.Add(new
                                {
                                    PtAccount = reader["ptaccount"],
                                    PtName = reader["ptname"],
                                    EncounterID = reader["encounterid"],
                                    VisitDate = reader["visitdate"],
                                    PreppedBy = reader["preppedby"],
                                    PreppedTime = reader["preppedtime"],
                                    VisitType = visitType
                                });
                            }
                        }
                    }

                    return Ok(new { Success = true, FacilityCount = facilities.Count, PatientCount = patients.Count, Data = patients });
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Success = false, Message = ex.Message });
            }
        }




    
    
    //    [HttpGet("screenings")]
    //     public IActionResult Screenings()
    //     {
    //         string connStr = _config.GetConnectionString("Huddle_V2");

    //         using (var conn = new MySqlConnection(connStr))
    //         {
    //             conn.Open();

    //             string q = @"
    //                 SELECT DISTINCT
    //                     g.GapID,
    //                     gg.name
    //                 FROM Huddle_V2_EHR_Patients_Gaps g
    //                 INNER JOIN Huddle_V2_Gaps gg ON g.gapID = gg.id
    //                 WHERE g.isActive = 1
    //                 AND g.Status = 'Open'
    //                 AND gg.Internal = 0
    //                 ORDER BY gg.name;
    //             ";

    //             using (var cmd = new MySqlCommand(q, conn))
    //             using (var reader = cmd.ExecuteReader())
    //             {
    //                 var result = new List<object>();

    //                 while (reader.Read())
    //                 {
    //                     result.Add(new
    //                     {
    //                         GapID = reader["GapID"],
    //                         Name = reader["name"]
    //                     });
    //                 }

    //                 return Ok(new { Success = true, Data = result });
    //             }
    //         }
    //     }
     }
}