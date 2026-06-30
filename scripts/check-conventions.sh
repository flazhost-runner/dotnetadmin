#!/usr/bin/env bash
set -e

echo "=== DotNetAdmin Convention Checker ==="
echo ""

FAIL=0

# 1. Build check (0 errors)
echo "[1/5] Building..."
BUILD_OUTPUT=$(dotnet build 2>&1)
BUILD_ERRORS=$(echo "$BUILD_OUTPUT" | grep -c " error CS" || true)
if [ "$BUILD_ERRORS" -gt 0 ]; then
  echo "FAIL: $BUILD_ERRORS build error(s)" >&2
  echo "$BUILD_OUTPUT" | grep " error CS" >&2
  FAIL=1
else
  echo "PASS: Build succeeded (0 errors)"
fi

# 2. Test check
echo "[2/5] Running tests..."
TEST_OUTPUT=$(dotnet test tests/DotNetAdmin.Tests/DotNetAdmin.Tests.csproj --no-build 2>&1)
if echo "$TEST_OUTPUT" | grep -qE "Failed:[[:space:]]+[1-9]"; then
  echo "FAIL: Tests failing" >&2
  FAIL=1
else
  echo "PASS: All tests passing"
fi

# 3. Interface naming: all interfaces must start with I
echo "[3/5] Checking interface naming (must start with I)..."
BAD_IFACE=$(grep -rn "^public interface [^I]" src/ --include="*.cs" || true)
if [ -n "$BAD_IFACE" ]; then
  echo "FAIL: Interfaces not starting with 'I':" >&2
  echo "$BAD_IFACE" >&2
  FAIL=1
else
  echo "PASS: Interface naming OK"
fi

# 4. No Directory.GetCurrentDirectory() in modules (use IWebHostEnvironment)
echo "[4/5] Checking no raw path access in modules..."
BAD_ENV=$(grep -rn "Directory\.GetCurrentDirectory()" src/Modules/ --include="*.cs" || true)
if [ -n "$BAD_ENV" ]; then
  echo "FAIL: Directory.GetCurrentDirectory() found in modules (use IWebHostEnvironment instead):" >&2
  echo "$BAD_ENV" >&2
  FAIL=1
else
  echo "PASS: No raw path access in modules"
fi

# 5. Service pattern: concrete *Service.cs (not I*Service.cs) must implement an interface
echo "[5/5] Checking service implements-interface pattern..."
BAD_SVC=""
while IFS= read -r f; do
  base=$(basename "$f")
  # Skip interface files (start with I) and cache files
  if [[ "$base" == I* ]]; then continue; fi
  if ! grep -q "class .* : I" "$f" 2>/dev/null; then
    BAD_SVC="$BAD_SVC\n$f"
  fi
done < <(find src/ -name "*Service.cs" -not -path "*/obj/*")
if [ -n "$BAD_SVC" ]; then
  echo "WARN: Service classes may not implement I*Service interface:"
  echo -e "$BAD_SVC"
else
  echo "PASS: Service interface pattern OK"
fi

echo ""
if [ "$FAIL" -eq 0 ]; then
  echo "=== All checks PASSED ==="
  exit 0
else
  echo "=== FAILED: $FAIL check(s) failed ===" >&2
  exit 1
fi
