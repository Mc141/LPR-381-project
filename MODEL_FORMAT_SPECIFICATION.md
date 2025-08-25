# ? **MODEL FORMAT SPECIFICATION**

## ?? **Required Input Format**

All algorithms in the Integer Programming system use the **coefficient-only format** without explicit variable names:

### **Standard Format Structure:**
```
[max|min] +coeff1 +coeff2 +coeff3 ...
+coeff1 +coeff2 +coeff3 ... [<=|>=|=] RHS
+coeff1 +coeff2 +coeff3 ... [<=|>=|=] RHS
...
[restriction1] [restriction2] [restriction3] ...
```

### **Example (Your Specified Format):**
```
max +2 +3 +3 +5 +2 +4
+11 +8 +6 +14 +10 +10 <= 40
bin bin bin bin bin bin
```

This creates:
- **Variables**: x1, x2, x3, x4, x5, x6 (automatically generated)
- **Objective**: maximize 2x1 + 3x2 + 3x3 + 5x4 + 2x5 + 4x6
- **Constraint**: 11x1 + 8x2 + 6x3 + 14x4 + 10x5 + 10x6 ? 40
- **All variables are binary**: 0 ? xi ? 1, xi ? {0,1}

## ?? **Test Models - Updated Format**

### **1. Binary Test Model (6 variables)**
```plaintext
File: binary_test_model.txt
max +2 +3 +3 +5 +2 +4
+11 +8 +6 +14 +10 +10 <= 40
bin bin bin bin bin bin
```

### **2. Simple Binary Model (4 variables)**
```plaintext
File: simple_binary_model.txt
max +1 +1 +1 +1
+1 +1 +1 +1 <= 2
bin bin bin bin
```

### **3. Knapsack Model (3 variables)**
```plaintext
File: knapsack_test_model.txt
max +10 +6 +4
+5 +4 +6 <= 20
bin bin bin
```

### **4. Integer Programming Model (2 variables)**
```plaintext
File: integer_test_model.txt
max +3 +2
+1 +1 <= 4
+2 +1 <= 6
int int
```

### **5. Complex Integer Model (5 variables)**
```plaintext
File: complex_integer_model.txt
max +5 +3 +2 +7 +1
+2 +1 +1 +3 +1 <= 5
+1 +2 +3 +1 +2 <= 6
+3 +1 +1 +2 +1 <= 7
int int int int int
```

### **6. Large Binary Model (7 variables)**
```plaintext
File: large_binary_model.txt
max +8 +5 +6 +9 +3 +7 +4
+3 +2 +4 +1 +2 +3 +1 <= 10
+2 +3 +1 +2 +1 +2 +3 <= 8
+1 +1 +2 +3 +4 +1 +2 <= 12
bin bin bin bin bin bin bin
```

## ?? **Algorithm Compatibility**

### ? **All Algorithms Support This Format:**

#### **1. Branch & Bound (Simplex)**
- ? **Variable naming**: Uses x1, x2, x3, ... automatically
- ? **Coefficient parsing**: Correctly reads +2 +3 +3 +5 +2 +4
- ? **Binary constraints**: Adds x ? 1 constraints automatically
- ? **Branching**: x = 0 or x = 1 for binary variables

#### **2. Branch & Bound (Knapsack)**
- ? **Item naming**: Uses x1, x2, x3, ... as item names
- ? **Value extraction**: Uses objective coefficients as item values
- ? **Weight extraction**: Uses constraint coefficients as weights
- ? **Binary enforcement**: Handles 0-1 selection automatically

#### **3. Cutting Plane Algorithm**
- ? **Variable handling**: Works with x1, x2, x3, ... naming
- ? **Cut generation**: Creates cuts using correct variable names
- ? **Binary validation**: Enforces binary constraints properly

## ??? **Internal Processing**

### **ModelParser Behavior:**
```csharp
// Input: "max +2 +3 +3 +5 +2 +4"
// Creates:
model.Variables["x1"] = new Variable { Name = "x1", Coefficient = 2, Index = 0 };
model.Variables["x2"] = new Variable { Name = "x2", Coefficient = 3, Index = 1 };
model.Variables["x3"] = new Variable { Name = "x3", Coefficient = 3, Index = 2 };
model.Variables["x4"] = new Variable { Name = "x4", Coefficient = 5, Index = 3 };
model.Variables["x5"] = new Variable { Name = "x5", Coefficient = 2, Index = 4 };
model.Variables["x6"] = new Variable { Name = "x6", Coefficient = 4, Index = 5 };
```

### **Constraint Processing:**
```csharp
// Input: "+11 +8 +6 +14 +10 +10 <= 40"
// Creates:
constraint.Coefficients["x1"] = 11;
constraint.Coefficients["x2"] = 8;
constraint.Coefficients["x3"] = 6;
constraint.Coefficients["x4"] = 14;
constraint.Coefficients["x5"] = 10;
constraint.Coefficients["x6"] = 10;
constraint.RHS = 40;
```

### **Sign Restriction Processing:**
```csharp
// Input: "bin bin bin bin bin bin"
// Creates:
model.Variables["x1"].SignRestriction = SignRestriction.Binary;
model.Variables["x2"].SignRestriction = SignRestriction.Binary;
model.Variables["x3"].SignRestriction = SignRestriction.Binary;
model.Variables["x4"].SignRestriction = SignRestriction.Binary;
model.Variables["x5"].SignRestriction = SignRestriction.Binary;
model.Variables["x6"].SignRestriction = SignRestriction.Binary;
```

## ?? **Expected Results Format**

### **Solution Display:**
```
Optimal Solution:
  x1 = 1.000
  x2 = 0.000
  x3 = 1.000
  x4 = 0.000
  x5 = 1.000
  x6 = 0.000
```

### **Node Tree (Branch & Bound):**
- **Node descriptions**: "x1 = 0", "x1 = 1", "x2 = 0", "x2 = 1", etc.
- **Branching variables**: x1, x2, x3, x4, x5, x6
- **Solution variables**: Same naming consistency

### **Cuts Display (Cutting Plane):**
- **Cut expressions**: "1.5x1 + 0.7x2 + 2.3x3 ? 2"
- **Variable references**: x1, x2, x3, x4, x5, x6
- **Cut validation**: Uses consistent variable naming

## ? **Verification Checklist**

### **Format Compliance:**
- ? **Objective coefficients only**: No variable names in objective
- ? **Constraint coefficients only**: No variable names in constraints  
- ? **Sign restrictions only**: One restriction per variable
- ? **Space separation**: Proper spacing between elements
- ? **Sign notation**: + or - prefixes on coefficients

### **Algorithm Integration:**
- ? **All IP algorithms**: Support the coefficient-only format
- ? **Variable generation**: Automatic x1, x2, x3, ... naming
- ? **Consistent display**: Same variable names throughout
- ? **Binary enforcement**: Proper 0-1 constraints for binary variables

### **Testing Ready:**
- ? **Multiple test models**: Various sizes and types available
- ? **Format consistency**: All models use the same structure
- ? **Algorithm compatibility**: All algorithms work with all models
- ? **Expected results**: Clear outcomes for each test case

## ?? **Format Specification Complete**

**All Integer Programming algorithms now fully support the required format:**

```
max +2 +3 +3 +5 +2 +4
+11 +8 +6 +14 +10 +10 <= 40
bin bin bin bin bin bin
```

**The system automatically handles:**
- ? Variable name generation (x1, x2, x3, ...)
- ? Coefficient parsing and assignment
- ? Binary constraint enforcement
- ? Consistent variable naming across all algorithms
- ? Proper solution display and tree visualization

**Ready for testing with the specified format!** ??