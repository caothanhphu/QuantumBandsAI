#!/bin/bash

# SCRUM-70 API Endpoint Testing Script
# Testing the Account Overview API endpoint

echo "🚀 Testing SCRUM-70: Account Overview API Endpoint"
echo "=================================================="

BASE_URL="https://localhost:7232"
API_ENDPOINT="/api/v1/trading-accounts/1/overview"

echo ""
echo "📍 Endpoint: GET ${BASE_URL}${API_ENDPOINT}"
echo ""

# Test 1: Test without authentication (should return 401)
echo "🧪 Test 1: Request without authentication (expecting 401 Unauthorized)"
echo "-------------------------------------------------------------------"
curl -k -s -w "\nHTTP Status: %{http_code}\n" \
     -H "Content-Type: application/json" \
     "${BASE_URL}${API_ENDPOINT}"

echo ""
echo ""

# Test 2: Test with invalid token (should return 401)
echo "🧪 Test 2: Request with invalid token (expecting 401 Unauthorized)"
echo "-----------------------------------------------------------------"
curl -k -s -w "\nHTTP Status: %{http_code}\n" \
     -H "Content-Type: application/json" \
     -H "Authorization: Bearer invalid-token-here" \
     "${BASE_URL}${API_ENDPOINT}"

echo ""
echo ""

# Test 3: Test API endpoint structure (check if endpoint exists)
echo "🧪 Test 3: Check API endpoint availability"
echo "----------------------------------------"
curl -k -s -w "\nHTTP Status: %{http_code}\n" \
     -X OPTIONS \
     "${BASE_URL}${API_ENDPOINT}"

echo ""
echo ""

# Test 4: Check Swagger documentation
echo "🧪 Test 4: Check Swagger Documentation"
echo "-------------------------------------"
echo "Swagger UI should be available at: ${BASE_URL}/swagger"
curl -k -s -w "\nHTTP Status: %{http_code}\n" \
     "${BASE_URL}/swagger/index.html" | head -1

echo ""
echo ""

# Test 5: Test with non-existent account ID
echo "🧪 Test 5: Request with non-existent account ID (expecting 404)"
echo "--------------------------------------------------------------"
curl -k -s -w "\nHTTP Status: %{http_code}\n" \
     -H "Content-Type: application/json" \
     "${BASE_URL}/api/v1/trading-accounts/99999/overview"

echo ""
echo ""

echo "✅ Testing completed!"
echo ""
echo "📝 Test Summary:"
echo "- All requests should return proper HTTP status codes"
echo "- 401 for unauthorized requests (Tests 1 & 2)"  
echo "- 404 for non-existent resources (Test 5)"
echo "- Swagger documentation should be accessible (Test 4)"
echo ""
echo "🔐 To test authenticated requests, you need to:"
echo "1. Register/login to get a valid JWT token"
echo "2. Use the token in Authorization header"
echo "3. Test with valid account IDs that the user owns"
echo ""
echo "📖 For full testing with authentication, refer to:"
echo "   - Postman collection in /Postman/ directory"
echo "   - API documentation at ${BASE_URL}/swagger"
