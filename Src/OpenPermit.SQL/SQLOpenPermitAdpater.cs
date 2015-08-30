using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenPermit.SQL
{
    public class SQLOpenPermitAdpater: IOpenPermitAdapter
    {

        public List<Permit> SearchPermits(PermitFilter filter)
        {
            throw new NotImplementedException();
        }

        public Permit GetPermit(string permitNumber)
        {
            throw new NotImplementedException();
        }

        public List<PermitStatus> GetPermitTimeline(string permitNumber)
        {
            throw new NotImplementedException();
        }

        public List<Inspection> GetInspections(string permitNumber)
        {
            throw new NotImplementedException();
        }

        public Inspection GetInspection(string permitNumber, string inspectionId)
        {
            throw new NotImplementedException();
        }

        public Attachment GetInspectionAttachment(string permitNumber, string inspectionId, string attachmentId)
        {
            throw new NotImplementedException();
        }

        public List<Contractor> GetContractors(string permitNumber)
        {
            throw new NotImplementedException();
        }

        public Contractor GetContractor(string permitNumber, string contractorId)
        {
            throw new NotImplementedException();
        }
    }
}
