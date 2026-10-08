using Microsoft.AspNetCore.Mvc;
using System.Data;
using MySql.Data.MySqlClient;
using Horizon.Repository.ECW;

namespace prepchartApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
     public class ChartController : ControllerBase
     {

        [HttpGet("Patients")]
        public IActionResult Patients(int? dID , int? fID ,DateTime date)
        {
            string formattedDate = date.ToString("yyyy-MM-dd");
            string query = @"            
                SELECT 
                    u.ufname,
                    u.ulname,
                    p.ControlNo,
                    u.uid
                FROM patients p
                JOIN users u ON u.uid = p.pid
                JOIN enc e ON e.PatientID = p.pid
                JOIN doctors d ON d.doctorID = e.doctorID
                JOIN edi_facilities f ON f.Id = e.facilityId
                WHERE e.date = '" + formattedDate + @"'
                    AND e.encType = 1
                    AND e.`STATUS` NOT IN ('canc', 'cancphone', 'n/s', 'cancsms')
                    AND e.deleteFlag = 0
                    AND d.inactive = 0
                    AND u.status = 0";
            if (fID.HasValue)
                query += " AND f.Id ='" + fID + @"'";
            else if (dID.HasValue)
                query += " AND d.doctorID = '" + dID + @"'";
             return RunQueryAndReturn(query);
        }


        [HttpGet("Providers")]
        public IActionResult PastProviders(int controlNo)
        {
            var q = $"CALL HMGGetProviderVisits({controlNo})";
             return RunQueryAndReturn(q);
        }

        [HttpGet("CircleofCare")]
        public IActionResult CircleofCare(int controlNo)
        {
            
            string query = @"            
                SELECT
                        coc.name AS cocName,
                        CONCAT(u.ufname, ' ', u.ulname) AS fullName,
                        d.speciality
                        FROM coc_contacts cc
                        INNER JOIN coc ON cc.coc_id = coc.id AND coc.delflag = 0
                        LEFT JOIN patients p ON coc.patientid = p.pid
                        LEFT JOIN users u ON cc.contactid = u.uid
                        LEFT JOIN doctors d ON cc.contactid = d.doctorID
                        WHERE p.ControlNo = '"+ controlNo + @"'
                        AND cc.delflag = 0;
                        ";
           
             return RunQueryAndReturn(query);
        }



        [HttpGet("Documents")]
        public IActionResult Documents(int pcontrolno)
        {
             
             var q = $"CALL HMGGetDocuments({pcontrolno})";
             return RunQueryAndReturn(q); 
        }

        [HttpGet("Referrals")]
        public IActionResult Referrals(int pcontrolno)
        {
            
            var q = $"CALL HMGGetAllOutgoingRef({pcontrolno})";
             return RunQueryAndReturn(q); 
        }

        [HttpGet("OpenOrders")]
         public IActionResult OpenOrders(int controlNo)
          {

             string query = @"SELECT
						p.ControlNo,
						cvx.cvx,
						TIMESTAMP(i.givenDate, i.GivenTime) givenDateTime,
						IFNULL(cvx.VaccineName,i.VaccineName) vaccineName,
						CASE
						WHEN i.immstatus = 1 THEN 'Administered'
						WHEN i.immstatus = 2 THEN 'Partially Administered'
						WHEN i.immstatus IN (4,6,7) THEN 'Not Administered'
						WHEN i.immstatus = 0 THEN 'Pending'
						ELSE i.immstatus
						END immstatus,
						i.givenBy
							FROM immunizations i
							LEFT JOIN patients p ON i.patientId = p.pid
							LEFT JOIN cptcvx cvx ON i.itemID = cvx.itemid
								WHERE p.ControlNo = '"+ controlNo + @"'
								AND i.deleteFlag = 0
									ORDER BY givenDateTime DESC";
            return RunQueryAndReturn(query);
               
         }
         [HttpGet("ClosedOrders")]
        public IActionResult ClosedOrders(int pcontrolno)
        { 
            string query = $"CALL  HMGGetAllOpenAndLatestClosedOrders({pcontrolno})";
            return RunQueryAndReturn(query);
        }

        


        // 🔁 Shared method to reduce duplication
        private IActionResult RunQueryAndReturn(string query)
        {
            var results = new ECWDB().RunQuery(query);
            var dictList = new List<Dictionary<string, object>>();

            foreach (DataRow row in results.Rows)
            {
                var rowDict = new Dictionary<string, object>();
                foreach (DataColumn col in results.Columns)
                {
                    rowDict[col.ColumnName] = row[col];
                }
                dictList.Add(rowDict);
            }

            return Ok(dictList);
        }
     }
}