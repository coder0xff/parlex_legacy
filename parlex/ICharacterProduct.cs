using System;

namespace parlex {
    public interface ICharacterProduct {
        bool Match(Int32 codePoint);
        Int32 GetExampleCodePoint();
    }
}
