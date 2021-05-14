using Newtonsoft.Json;

namespace SafeAccountsAPI.Models {
    public class Folder {
        public int ID { get; set; }
        public int UserID { get; set; }
        public virtual User User { get; set; }
        public int? ParentID { get; set; }
        public virtual Folder Parent { get; set; }
        public string FolderName { get; set; }
        public bool HasChild { get; set; }

        public Folder() { } // blank constructor needed for db initializer

        // constructor to easily set from NewFolder type
        public Folder(NewFolder newFolder, int uid)
        {
            FolderName = newFolder.Folder_Name;
            UserID = uid;
            ParentID = newFolder.Parent_ID;
            HasChild = false;
        }
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

    // model for registering a new user
    public class NewFolder
    {
        [JsonProperty]
        public string Folder_Name { get; set; }
        [JsonProperty]
        public int? Parent_ID { get; set; }
    }
}
