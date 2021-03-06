﻿using System;

namespace Parlex {
    [Serializable]
    public class UndefinedProductionException : Exception {
        public String ProductionName { get; private set; }
        public UndefinedProductionException(string name) {
            ProductionName = name;
        }
    }
}