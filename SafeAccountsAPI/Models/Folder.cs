using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SafeAccountsAPI.Models {
    public class Folder {
        public int ID { get; set; }
        public int UserID { get; set; }
        public virtual User User { get; set; }
        public int? ParentID { get; set; }
        public virtual Folder Parent { get; set; }
        public string FolderName { get; set; }
        public bool HasChild { get; set; }
    }

    public class ReturnableFolder {

        public int ID { get; set; }
        public string FolderName { get; set; }
        public int? ParentID { get; set; }
        public bool HasChild { get; set; }

        public ReturnableFolder(Folder fold)
        {
            ID = fold.ID;
            FolderName = fold.FolderName;
            HasChild = fold.HasChild;

            if (fold.ParentID != null)
                ParentID = fold.ParentID;
        }
    }
}
