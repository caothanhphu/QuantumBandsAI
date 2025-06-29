# 🧪 TEST SCENARIOS - MT5 C# Migration

## 📋 **Test Strategy Overview**

### **Test Levels:**
1. **Unit Tests** - Test individual components
2. **Integration Tests** - Test component interactions  
3. **System Tests** - Test end-to-end workflows
4. **Performance Tests** - Benchmark against Python version
5. **User Acceptance Tests** - Verify business requirements

---

## 🎯 **Functional Test Scenarios**

### **TS-001: MT5 Connection Management**

#### **Test Case 1.1: Single Account Connection**
```
GIVEN: Valid MT5 login credentials
WHEN: Initialize MT5 connection
THEN: 
  ✅ Connection established successfully
  ✅ Account info retrieved correctly
  ✅ No memory leaks after connection
```

#### **Test Case 1.2: Multiple Account Connections**
```
GIVEN: 3 valid MT5 accounts in config
WHEN: Initialize all connections sequentially
THEN:
  ✅ All 3 connections successful
  ✅ Each account data isolated correctly
  ✅ Parallel connection handling works
```

#### **Test Case 1.3: Invalid Credentials**
```
GIVEN: Invalid login/password/server
WHEN: Attempt to connect
THEN:
  ✅ Connection fails gracefully
  ✅ Proper error message logged
  ✅ No hanging connections
  ✅ Retry logic activated (if configured)
```

#### **Test Case 1.4: Connection Recovery**
```
GIVEN: Established MT5 connection
WHEN: Network interruption occurs
THEN:
  ✅ Connection drop detected
  ✅ Automatic reconnection attempted
  ✅ Data retrieval resumes after reconnect
  ✅ No data loss during reconnection
```

### **TS-002: Account Information Retrieval**

#### **Test Case 2.1: Basic Account Info**
```
GIVEN: Connected MT5 account
WHEN: Request account information
THEN:
  ✅ Balance retrieved correctly
  ✅ Equity retrieved correctly
  ✅ Data format matches API expectations
  ✅ Response time < 1 second
```

#### **Test Case 2.2: Account Info Accuracy**
```
GIVEN: Known account state in MT5
WHEN: Retrieve account info via C# service
THEN:
  ✅ Balance matches MT5 terminal display
  ✅ Equity matches MT5 terminal display
  ✅ Currency information correct
  ✅ Decimal precision maintained
```

### **TS-003: Open Positions Retrieval**

#### **Test Case 3.1: No Open Positions**
```
GIVEN: Account with no open positions
WHEN: Request open positions
THEN:
  ✅ Empty list returned
  ✅ No errors or exceptions
  ✅ Proper logging of empty result
```

#### **Test Case 3.2: Multiple Open Positions**
```
GIVEN: Account with 5 open positions
WHEN: Request open positions
THEN:
  ✅ All 5 positions retrieved
  ✅ Each position has complete data:
    - Ticket ID
    - Symbol
    - Trade type (Buy/Sell)
    - Volume
    - Open price/time
    - Current market price
    - Swap/commission
    - Floating P&L
```

#### **Test Case 3.3: Position Data Accuracy**
```
GIVEN: Known open position in MT5
WHEN: Retrieve via C# service
THEN:
  ✅ All fields match MT5 terminal exactly
  ✅ Timestamp converted to UTC correctly
  ✅ ISO 8601 format maintained
  ✅ Floating P&L calculated correctly
```

### **TS-004: Closed Trades History**

#### **Test Case 4.1: Recent Closed Trades**
```
GIVEN: Lookback period of 24 hours
AND: 3 trades closed in last 24h
WHEN: Request closed trades
THEN:
  ✅ All 3 trades retrieved
  ✅ Trade data complete and accurate
  ✅ Deal pairing logic works correctly
  ✅ Realized P&L calculated correctly
```

#### **Test Case 4.2: No Closed Trades**
```
GIVEN: Lookback period with no closed trades
WHEN: Request closed trades history
THEN:
  ✅ Empty list returned
  ✅ No errors or warnings
  ✅ Proper logging of no data
```

#### **Test Case 4.3: Deal Pairing Logic**
```
GIVEN: Complex trade with partial closes
WHEN: Retrieve closed trades
THEN:
  ✅ Entry/exit deals paired correctly
  ✅ No duplicate trade records
  ✅ Volume calculations accurate
  ✅ Commission/swap aggregated properly
```

### **TS-005: API Communication**

#### **Test Case 5.1: Successful API Calls**
```
GIVEN: Valid API endpoint and key
WHEN: Send live data to API
THEN:
  ✅ HTTP 200/202 response received
  ✅ Request completed within timeout
  ✅ Proper headers sent (API key, content-type)
  ✅ JSON payload formatted correctly
```

#### **Test Case 5.2: API Error Handling**
```
GIVEN: API returns 500 error
WHEN: Send data request
THEN:
  ✅ Error logged appropriately
  ✅ Retry logic triggered
  ✅ Max retries respected
  ✅ Service continues after failure
```

#### **Test Case 5.3: Network Timeout**
```
GIVEN: API endpoint responds slowly
WHEN: Request timeout exceeded
THEN:
  ✅ Request cancelled properly
  ✅ Timeout error logged
  ✅ Retry attempted (if configured)
  ✅ No hanging connections
```

---

## ⚡ **Performance Test Scenarios**

### **PS-001: Memory Usage Testing**

#### **Test Case P1.1: Memory Baseline**
```
TEST: Run C# service for 1 hour
MEASURE: Memory consumption every 5 minutes
EXPECTED: 
  ✅ Initial memory < 30MB
  ✅ No memory leaks detected
  ✅ Memory growth < 5MB/hour
  ✅ Garbage collection working properly
```

#### **Test Case P1.2: Memory vs Python**
```
TEST: Compare C# vs Python under same load
MEASURE: Peak memory usage
EXPECTED:
  ✅ C# memory <= Python memory
  ✅ C# memory more stable over time
  ✅ C# faster garbage collection
```

### **PS-002: CPU Usage Testing**

#### **Test Case P2.1: CPU Efficiency**
```
TEST: Run service with 5 MT5 accounts
MEASURE: CPU usage during peak operations
EXPECTED:
  ✅ Average CPU < 5%
  ✅ Peak CPU < 15%
  ✅ No CPU spikes > 3 seconds
```

### **PS-003: Response Time Testing**

#### **Test Case P3.1: Data Retrieval Speed**
```
TEST: Time each MT5 operation
MEASURE: Response times for 100 iterations
EXPECTED:
  ✅ Account info: < 500ms avg
  ✅ Open positions: < 1s avg
  ✅ Closed trades: < 2s avg
  ✅ 95th percentile < 3s
```

### **PS-004: Concurrent Load Testing**

#### **Test Case P4.1: Multiple Accounts**
```
TEST: 10 MT5 accounts running simultaneously
MEASURE: System performance and data accuracy
EXPECTED:
  ✅ All accounts processed successfully
  ✅ No data corruption or mixing
  ✅ Response times remain acceptable
  ✅ System stable for 24+ hours
```

---

## 🔄 **Regression Test Scenarios**

### **RS-001: Data Accuracy vs Python**

#### **Test Case R1.1: Side-by-Side Comparison**
```
SETUP: Run Python and C# versions in parallel
TEST: Compare outputs for 1 week
VALIDATE:
  ✅ Account balances identical
  ✅ Position data matches 100%
  ✅ Closed trades data identical
  ✅ Timestamps synchronized
  ✅ API payloads equivalent
```

#### **Test Case R1.2: Edge Cases**
```
TEST: Handle edge cases that occurred in Python
SCENARIOS:
  ✅ Very large position counts (>100)
  ✅ High-frequency trading accounts
  ✅ Currency pairs with exotic names
  ✅ Very small lot sizes (0.01)
  ✅ Very old closed trades
```

---

## 🚨 **Error Handling Test Scenarios**

### **ES-001: MT5 Connection Errors**

#### **Test Case E1.1: MT5 Terminal Closed**
```
GIVEN: C# service running
WHEN: MT5 terminal is closed
THEN:
  ✅ Connection error detected immediately
  ✅ Proper error message logged
  ✅ Service attempts reconnection
  ✅ Service doesn't crash
```

#### **Test Case E1.2: Invalid Server**
```
GIVEN: Config with invalid MT5 server
WHEN: Attempt connection
THEN:
  ✅ Connection fails with clear error
  ✅ Error logged with details
  ✅ Other accounts continue working
  ✅ No infinite retry loops
```

### **ES-002: API Service Errors**

#### **Test Case E2.1: API Server Down**
```
GIVEN: API server unavailable
WHEN: Attempt to send data
THEN:
  ✅ Connection error detected
  ✅ Retry logic activated
  ✅ Data queued for retry (if applicable)
  ✅ Service continues other operations
```

#### **Test Case E2.2: Invalid API Key**
```
GIVEN: Incorrect API key in config
WHEN: Send API request
THEN:
  ✅ 401/403 error handled properly
  ✅ Clear authentication error logged
  ✅ No sensitive data in logs
  ✅ Service doesn't retry indefinitely
```

---

## 📊 **Test Data Management**

### **Test Environment Setup:**
```json
{
  "demo_accounts": [
    {
      "login": "12345678",
      "password": "demo_pass_1",
      "server": "MetaQuotes-Demo",
      "description": "Basic test account"
    },
    {
      "login": "87654321", 
      "password": "demo_pass_2",
      "server": "MetaQuotes-Demo",
      "description": "High-frequency test account"
    }
  ],
  "test_api": {
    "base_url": "http://localhost:5000/api/test",
    "api_key": "test_key_12345",
    "timeout": 30
  }
}
```

### **Test Data Validation:**
- **Account Info**: Known balance/equity values
- **Positions**: Pre-created test positions
- **History**: Known closed trades to verify
- **Symbols**: Standard pairs (EURUSD, GBPUSD, etc.)

---

## ✅ **Test Execution Checklist**

### **Pre-Testing:**
- [ ] Test environment configured
- [ ] Demo accounts created and verified
- [ ] Test API server running
- [ ] Python version baseline captured
- [ ] Test data prepared

### **During Testing:**
- [ ] All automated tests executed
- [ ] Manual test scenarios completed
- [ ] Performance benchmarks captured
- [ ] Error scenarios validated
- [ ] Regression tests passed

### **Post-Testing:**
- [ ] Test results documented
- [ ] Performance comparison completed
- [ ] Issues identified and tracked
- [ ] Test coverage validated (>80%)
- [ ] UAT sign-off obtained

---

## 📈 **Success Criteria**

### **Functional:**
- [ ] 100% of Python functionality replicated
- [ ] All data accuracy tests pass
- [ ] Error handling comprehensive
- [ ] Configuration compatibility maintained

### **Performance:**
- [ ] Memory usage <= Python version
- [ ] Response times improved or equal
- [ ] CPU usage optimized
- [ ] No performance regressions

### **Quality:**
- [ ] Unit test coverage >= 80%
- [ ] Integration tests pass 100%
- [ ] No critical or high severity bugs
- [ ] Code review approval obtained

---

**Test Plan Created**: {current_date}  
**Test Lead**: [Tên QA Lead]  
**Status**: Ready for Execution 