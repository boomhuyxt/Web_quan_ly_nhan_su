using System.Collections.Generic;

namespace Web_quan_ly_nhan_su.Models
{
    public class CreateGroupRequest
    {
        public string GroupName { get; set; }
        public int CreatorId { get; set; }
        public List<int> MemberIds { get; set; }
    }
}