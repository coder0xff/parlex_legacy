using System;

namespace parlex {
    public interface IBuiltInCharacterProduct {
        bool Match(Int32 codePoint);
        Int32 GetExampleCodePoint();
    }
}
