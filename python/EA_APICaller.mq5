//+------------------------------------------------------------------+
//|                                         EA_APICaller.mq5 |
//|                        Copyright 2025, QuantumBands AI User |
//|                           http://finixai.mywire.org |
//+------------------------------------------------------------------+
#property copyright "Copyright 2025, QuantumBands AI User"
#property link      "http://finixai.mywire.org"
#property version   "1.00"

//--- Input parameters
input string InpBaseURL = "http://finixai.mywire.org:6020"; // Base URL của API
input int    InpTradingAccountId = 1;                      // ID tài khoản giao dịch trong hệ thống của bạn
input string InpApiKey = "YOUR_SECRET_API_KEY";            // API Key để xác thực
input int    InpTimerIntervalSeconds = 3;                  // Khoảng thời gian gọi API (giây)
input int    InpMaxClosedTradesHistory = 10;               // Số lượng lệnh đã đóng gần nhất để lấy
input int    InpMaxRetries = 3;                            // Số lần thử lại khi API call thất bại
input int    InpRequestTimeout = 10000;                    // Timeout cho API request (ms)
input bool   InpEnableDetailedLogging = true;             // Bật logging chi tiết

//--- Global variables
ulong ExtExpertHandle = 0;
datetime g_last_api_call_time = 0;
int g_retry_count = 0;

//+------------------------------------------------------------------+
//| Expert initialization function                                 |
//+------------------------------------------------------------------+
int OnInit()
{
   //---
   Print("EA_APICaller initializing...");
   Print("BaseURL: ", InpBaseURL);
   Print("TradingAccountId (System): ", InpTradingAccountId);
   Print("Timer Interval: ", InpTimerIntervalSeconds, " seconds");
   Print("Max Closed Trades History: ", InpMaxClosedTradesHistory);
   Print("Max Retries: ", InpMaxRetries);
   Print("Request Timeout: ", InpRequestTimeout, " ms");

   // Thiết lập timer
   EventSetTimer(InpTimerIntervalSeconds);
   g_last_api_call_time = TimeCurrent();
   
   // Validate cấu hình
   if(InpBaseURL == "" || InpApiKey == "" || InpApiKey == "YOUR_SECRET_API_KEY")
   {
      Print("ERROR: Invalid configuration. Please check BaseURL and ApiKey.");
      return(INIT_FAILED);
   }
   
   if(InpTradingAccountId <= 0)
   {
      Print("ERROR: Invalid TradingAccountId. Must be greater than 0.");
      return(INIT_FAILED);
   }
   
   Print("EA_APICaller initialized successfully.");
   //---
   
   return(INIT_SUCCEEDED);
}
//+------------------------------------------------------------------+
//| Expert deinitialization function                               |
//+------------------------------------------------------------------+
void OnDeinit(const int reason)
{
   //---
   EventKillTimer();
   Print("EA_APICaller deinitialized. Reason: ", reason);
   //---
}
//+------------------------------------------------------------------+
//| Expert tick function                                           |
//+------------------------------------------------------------------+
void OnTick()
{
   //---
   // Logic giao dịch chính của bạn có thể đặt ở đây (nếu có)
   // Việc gọi API sẽ được xử lý trong OnTimer
   //---
}
//+------------------------------------------------------------------+
//| Timer function                                                 |
//+------------------------------------------------------------------+
void OnTimer()
{
   //---
   // Đảm bảo không gọi API quá thường xuyên nếu timer bị trigger nhanh hơn dự kiến
   if(TimeCurrent() - g_last_api_call_time < InpTimerIntervalSeconds)
   {
      return;
   }
   g_last_api_call_time = TimeCurrent();

   Print("Timer event - Calling APIs...");

   // Lấy thông tin tài khoản và các lệnh đang mở
   string liveDataJson = GetLiveDataJSON();
   if(liveDataJson != "")
   {
      string liveDataUrl = InpBaseURL + "/api/v1/ea-integration/trading-accounts/" + (string)InpTradingAccountId + "/live-data";
      SendDataToAPI(liveDataUrl, liveDataJson); // Đã bỏ comment
   }
   else
   {
      Print("Failed to generate live data JSON.");
   }

   // Lấy các lệnh đã đóng
   string closedTradesJson = GetClosedTradesJSON(InpMaxClosedTradesHistory);
   if(closedTradesJson != "")
   {
      string closedTradesUrl = InpBaseURL + "/api/v1/ea-integration/trading-accounts/" + (string)InpTradingAccountId + "/closed-trades";
      SendDataToAPI(closedTradesUrl, closedTradesJson); // Đã bỏ comment
   }
   else
   {
      Print("Failed to generate closed trades JSON.");
   }
   //---
}

//+------------------------------------------------------------------+
//| Lấy dữ liệu tài khoản và các lệnh đang mở dưới dạng JSON       |
//+------------------------------------------------------------------+
string GetLiveDataJSON()
{
   string json = "{";
   string positionsJson = "";
   int totalPositions = 0;

   // Lấy thông tin tài khoản - BỔ SUNG QUAN TRỌNG
   double equity, balance;
   equity = AccountInfoDouble(ACCOUNT_EQUITY);
   balance = AccountInfoDouble(ACCOUNT_BALANCE);
   
   // Thêm thông tin tài khoản vào JSON - THIẾU TRONG BẢN CŨ
   json += "\"accountEquity\":" + DoubleToString(equity, 2) + ",";
   json += "\"accountBalance\":" + DoubleToString(balance, 2) + ",";

   // Lấy các lệnh đang mở
   json += "\"openPositions\":[";
   for(int i = PositionsTotal() - 1; i >= 0; i--)
   {
      ulong ticket = PositionGetTicket(i);
      if(ticket > 0)
      {
         string symbol = PositionGetString(POSITION_SYMBOL);
         long type = PositionGetInteger(POSITION_TYPE); // 0 for Buy, 1 for Sell
         double volume = PositionGetDouble(POSITION_VOLUME);
         double openPrice = PositionGetDouble(POSITION_PRICE_OPEN);
         long timeOpen = PositionGetInteger(POSITION_TIME);
         double currentPrice = PositionGetDouble(POSITION_PRICE_CURRENT);
         double swap = PositionGetDouble(POSITION_SWAP);
         double commission = PositionGetDouble(POSITION_COMMISSION);
         double profit = PositionGetDouble(POSITION_PROFIT);

         if(totalPositions > 0)
         {
            positionsJson += ",";
         }

         positionsJson += "{";
         positionsJson += "\"eaTicketId\":\"" + (string)ticket + "\",";
         positionsJson += "\"symbol\":\"" + symbol + "\",";
         positionsJson += "\"tradeType\":\"" + (type == POSITION_TYPE_BUY ? "Buy" : "Sell") + "\",";
         positionsJson += "\"volumeLots\":" + DoubleToString(volume, 2) + ",";
         positionsJson += "\"openPrice\":" + DoubleToString(openPrice, 5) + ","; // Giá với 5 chữ số thập phân cho giá
         positionsJson += "\"openTime\":\"" + TimeToString(timeOpen, TIME_DATE | TIME_SECONDS) + "Z\","; // Thêm Z để chỉ UTC, format có thể cần điều chỉnh
         positionsJson += "\"currentMarketPrice\":" + DoubleToString(currentPrice, 5) + ",";
         positionsJson += "\"swap\":" + DoubleToString(swap, 2) + ",";
         positionsJson += "\"commission\":" + DoubleToString(commission, 2) + ",";
         positionsJson += "\"floatingPAndL\":" + DoubleToString(profit, 2);
         positionsJson += "}";
         totalPositions++;
      }
   }
   json += positionsJson;
   json += "]}";

   if(InpEnableDetailedLogging)
   {
      Print("Generated Live Data JSON: ", json);
   }
   return json;
}

//+------------------------------------------------------------------+
//| Lấy các lệnh đã đóng gần đây dưới dạng JSON                   |
//+------------------------------------------------------------------+
string GetClosedTradesJSON(int max_trades)
{
   string json = "{\"closedTrades\":[";
   int trades_count = 0;

   // Lấy lịch sử trong khoảng thời gian hoặc số lượng nhất định
   // Và dù lệnh lấy `max_trades` lệnh cuối cùng trong lịch sử tài khoản
   // Bạn cần một logic phức tạp hơn để chỉ lấy closed trades
   // Vì dữ này lấy `max_trades` lệnh cuối cùng trong lịch sử tài khoản
   if(HistorySelect(0, TimeCurrent())) // Chọn toàn bộ lịch sử
   {
      int totalDeals = HistoryDealsTotal();
      int dealsToFetch = MathMin(totalDeals, max_trades * 2); // Lấy nhiều deals hơn để lọc

      // Duyệt từ deals mới nhất về cũ
      for(int i = totalDeals - 1; i >= totalDeals - dealsToFetch && i >= 0; i--)
      {
         ulong deal_ticket = HistoryDealGetTicket(i);
         if(deal_ticket == 0)
            continue;

         long deal_type = HistoryDealGetInteger(deal_ticket, DEAL_TYPE);
         
         // Chỉ quan tâm đến deal đóng lệnh (DEAL_ENTRY_OUT)
         // Hoặc một position đã được đóng hoàn toàn.
         // Logic này cần được làm rất cẩn thận để đúng với từng trường hợp.
         // Đây là một ví dụ đơn giản, và có thể KHÔNG CHÍNH XÁC cho mọi trường hợp.
         
         if(HistoryDealGetInteger(deal_ticket, DEAL_ENTRY) == DEAL_ENTRY_OUT) // Deal đóng vị thế
         {
            // Tìm deal mở lệnh tương ứng để có OpenPrice, OpenTime, và xác định TradeType của lệnh gốc.
            // Đây là phần phức tạp.
            // Và dữ liệu RẤT ĐƠNG GIẢN và CÓ THỂ KHÔNG CHÍNH XÁC cho mọi trường hợp.
            // Bạn nên xem xét việc lấy thông tin từ History Orders và các deals liên quan đến position để
            // biết thêm chi tiết lỗi từ server, và có thể dùng DEAL_POSITION_ID để group các deals.

            if(HistoryDealGetInteger(deal_ticket, DEAL_ENTRY) == DEAL_ENTRY_OUT) // Deal đóng vị thế
            {
               // Lấy thông tin từ deal đóng
               string symbol = HistoryDealGetString(deal_ticket, DEAL_SYMBOL);
               long order_ticket_closed_by = HistoryDealGetInteger(deal_ticket, DEAL_ORDER); // Order ID của deal đóng
               long time_closed = HistoryDealGetInteger(deal_ticket, DEAL_TIME);
               double price_closed = HistoryDealGetDouble(deal_ticket, DEAL_PRICE);
               double volume_closed = HistoryDealGetDouble(deal_ticket, DEAL_VOLUME);
               double commission_closed = HistoryDealGetDouble(deal_ticket, DEAL_COMMISSION);
               double swap_closed = HistoryDealGetDouble(deal_ticket, DEAL_SWAP);
               double profit_closed = HistoryDealGetDouble(deal_ticket, DEAL_PROFIT);
               long type_closed = HistoryDealGetInteger(deal_ticket, DEAL_TYPE); // DEAL_TYPE_BUY or DEAL_TYPE_SELL
               long position_id = HistoryDealGetInteger(deal_ticket, DEAL_POSITION_ID); // Position ID để tìm deal mở

               // Tìm deal mở tương ứng với position này (implementation phức tạp)
               // Đây chỉ là placeholder logic đơn giản
               double open_price = 0;
               long open_time = time_closed - 3600; // Placeholder - giả sử đóng sau 1 giờ mở
               
               // BỔ SUNG: Logic tìm deal mở tương ứng
               for(int j = 0; j < totalDeals; j++)
               {
                  ulong open_deal_ticket = HistoryDealGetTicket(j);
                  if(open_deal_ticket == 0) continue;
                  
                  if(HistoryDealGetInteger(open_deal_ticket, DEAL_POSITION_ID) == position_id &&
                     HistoryDealGetInteger(open_deal_ticket, DEAL_ENTRY) == DEAL_ENTRY_IN)
                  {
                     open_price = HistoryDealGetDouble(open_deal_ticket, DEAL_PRICE);
                     open_time = HistoryDealGetInteger(open_deal_ticket, DEAL_TIME);
                     break;
                  }
               }

               // Tạo JSON cho trade đã đóng
               if(trades_count > 0)
               {
                  json += ",";
               }
               json += "{";
               json += "\"eaTicketId\":\"" + (string)order_ticket_closed_by + "\","; // Đây là order ID của deal đóng, có thể không phải ticket gốc
               json += "\"symbol\":\"" + symbol + "\",";
               json += "\"tradeType\":\"" + (type_closed == DEAL_TYPE_BUY ? "Buy" : "Sell") + "\","; // Đây là type của deal đóng, không phải lệnh gốc
               json += "\"volumeLots\":" + DoubleToString(volume_closed, 2) + ",";
               json += "\"openPrice\":" + DoubleToString(open_price, 5) + ","; // Sử dụng giá từ deal mở
               json += "\"openTime\":\"" + TimeToString(open_time, TIME_DATE | TIME_SECONDS) + "Z\","; // Sử dụng thời gian từ deal mở
               json += "\"closePrice\":" + DoubleToString(price_closed, 5) + ",";
               json += "\"closeTime\":\"" + TimeToString(time_closed, TIME_DATE | TIME_SECONDS) + "Z\",";
               json += "\"swap\":" + DoubleToString(swap_closed, 2) + ",";
               json += "\"commission\":" + DoubleToString(commission_closed, 2) + ",";
               json += "\"realizedPAndL\":" + DoubleToString(profit_closed, 2);
               json += "}";
               trades_count++;
               
               if(trades_count >= max_trades) break; // Giới hạn số lượng trades
            }
         }
      }
   }
   else
   {
      Print("HistorySelect failed, error: ", GetLastError());
   }

   json += "]}";
   if(trades_count == 0) return "{\"closedTrades\":[]}"; // Trả về mảng rỗng nếu không có
   
   if(InpEnableDetailedLogging)
   {
      Print("Generated Closed Trades JSON: ", json);
   }
   return json;
}

//+------------------------------------------------------------------+
//| Gửi dữ liệu JSON đến API                                       |
//+------------------------------------------------------------------+
void SendDataToAPI(string url, string jsonData)
{
   char post[], result[];
   string result_headers;
   int timeout = InpRequestTimeout;
   
   // Reset retry count for new API call
   static string last_url = "";
   static string last_data = "";
   if(last_url != url || last_data != jsonData)
   {
      g_retry_count = 0;
      last_url = url;
      last_data = jsonData;
   }

   // Chuẩn bị header
   string headers = "Content-Type: application/json\r\nX-API-KEY: " + InpApiKey + "\r\n";

   // Chuyển jsonData sang char array
   StringToCharArray(jsonData, post, 0, StringLen(jsonData), CP_UTF8);

   if(InpEnableDetailedLogging)
   {
      Print("Sending POST request to: ", url);
      Print("Payload length: ", StringLen(jsonData), " characters");
   }

   ResetLastError();
   int res = WebRequest("POST", url, headers, timeout, post, result, result_headers);

   if(res == -1)
   {
      int error_code = GetLastError();
      Print("WebRequest failed. Error code: ", error_code);
      
      // Retry logic
      if(g_retry_count < InpMaxRetries)
      {
         g_retry_count++;
         Print("Retrying API call (", g_retry_count, "/", InpMaxRetries, ") in 5 seconds...");
         Sleep(5000);
         SendDataToAPI(url, jsonData); // Recursive retry
         return;
      }
      else
      {
         Print("Max retries reached. API call failed permanently.");
         g_retry_count = 0; // Reset for next call
      }
      
      // Error code explanations
      // 4001 = ERR_WebRequest_INVALID_URL
      // 4003 = ERR_WebRequest_CONNECT_FAILED
      // 4004 = ERR_WebRequest_TIMEOUT
      // 4005 = ERR_WebRequest_REQUEST_FAILED
      // 4006 = ERR_WebRequest_HTTP_RETURN_CODE (lỗi từ server, ví dụ 400, 401, 500)
   }
   else if(res == 200 || res == 201 || res == 202) // Kiểm tra cả 202 Accepted
   {
      Print("Data sent successfully to ", url, ". Server response code: ", res);
      string response_text = CharArrayToString(result, 0, ArraySize(result) - 1, CP_UTF8);
      if(InpEnableDetailedLogging)
      {
         Print("Server response: ", response_text);
      }
      g_retry_count = 0; // Reset retry count on success
   }
   else // Các mã HTTP lỗi khác
   {
      Print("Failed to send data to ", url, ". HTTP status code: ", res);
      string response_text = CharArrayToString(result, 0, ArraySize(result) - 1, CP_UTF8);
      Print("Server response (error): ", response_text);
      Print("Response headers: ", result_headers);
      
      // Retry for server errors (5xx)
      if(res >= 500 && g_retry_count < InpMaxRetries)
      {
         g_retry_count++;
         Print("Server error detected. Retrying (", g_retry_count, "/", InpMaxRetries, ") in 10 seconds...");
         Sleep(10000);
         SendDataToAPI(url, jsonData);
         return;
      }
      g_retry_count = 0; // Reset for client errors (don't retry 4xx)
   }
}

//+------------------------------------------------------------------+
//| Chuyển đối thời gian Unix (long) sang chuỗi ISO 8601 (UTC)    |
//| MQL5 TimeToString không có 'Z' nên chúng ta thêm vào thủ công  |
//| Và đảm bảo format là YYYY-MM-DDTHH:mm:ss                       |
//+------------------------------------------------------------------+
string TimeToISO8601(long timestamp)
{
   MqlDateTime dt_struct;
   TimeToStruct(timestamp, dt_struct);
   return StringFormat("%04u-%02u-%02uT%02u:%02u:%02uZ",
                       dt_struct.year, dt_struct.mon, dt_struct.day,
                       dt_struct.hour, dt_struct.min, dt_struct.sec);
}