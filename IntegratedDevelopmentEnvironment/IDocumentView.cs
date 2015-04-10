using System;

namespace IntegratedDevelopmentEnvironment {
    interface IDocumentView {
        String FilePathName { get; set; }
        void LoadFromDisk();
        void SaveToDisk();
        void SaveCopy(String filePathName);
        bool HasUnsavedChanges { get; }
    }
}
