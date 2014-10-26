using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IntegratedDevelopmentEnvironment {
    interface IDocumentView {
        String FilePathName { get; set; }
        void LoadFromDisk();
        void SaveToDisk();
        void SaveCopy(String filePathName);
        bool HasUnsavedChanges { get; }
    }
}
