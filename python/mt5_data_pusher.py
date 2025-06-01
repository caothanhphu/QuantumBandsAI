import MetaTrader5 as mt5
import requests
import json
import time
import logging
from datetime import datetime, timedelta, timezone
import configparser # Để đọc file cấu hình
import os

# Thiết lập UTF-8 cho Windows để tránh lỗi encoding
os.environ['PYTHONIOENCODING'] = 'utf-8'

# --- Cấu hình Logging ---
logging.basicConfig(
    level=logging.INFO,
    format='%(asctime)s - %(levelname)s - %(filename)s:%(lineno)d - %(message)s',
    handlers=[
        logging.FileHandler("mt5_pusher.log", encoding='utf-8'), # Ghi log ra file với UTF-8 encoding
        logging.StreamHandler() # Ghi log ra console
    ]
)

# Cấu hình console handler để sử dụng UTF-8 trên Windows
console_handler = None
for handler in logging.getLogger().handlers:
    if isinstance(handler, logging.StreamHandler) and handler.stream.name == '<stderr>':
        console_handler = handler
        break

if console_handler and hasattr(console_handler.stream, 'reconfigure'):
    try:
        console_handler.stream.reconfigure(encoding='utf-8')
    except:
        # Nếu không thể reconfigure, thay thế console handler
        logging.getLogger().removeHandler(console_handler)
        new_console_handler = logging.StreamHandler()
        new_console_handler.setFormatter(logging.Formatter('%(asctime)s - %(levelname)s - %(filename)s:%(lineno)d - %(message)s'))
        logging.getLogger().addHandler(new_console_handler)

# --- CẤU HÌNH SCRIPT ---
CONFIG_FILE = 'mt5_config.ini'

# Hàm đọc cấu hình
def load_config(config_file=CONFIG_FILE):
    config = configparser.ConfigParser()
    if not config.read(config_file, encoding='utf-8'):
        logging.error(f"Lỗi: Không tìm thấy file cấu hình '{config_file}'. Vui lòng tạo file này.")
        logging.error("Ví dụ nội dung file mt5_config.ini:")
        logging.error("""
[General]
ApiBaseUrl = http://finixai.mywire.org:6020
ApiKey = YOUR_SECRET_API_KEY_HERE
TimeIntervalSeconds = 3
LookbackHoursClosedTrades = 24 ; Số giờ nhìn lại để lấy lệnh đã đóng (ví dụ)

[MT5Account_1]
TradingAccountIdSystem = 1 ; ID tài khoản trong hệ thống .NET của bạn
Login = 1234567
Password = YourMT5Password
Server = YourMT5Broker-Server
Enabled = true

; Thêm [MT5Account_2], [MT5Account_3], ... nếu có nhiều tài khoản
""")
        return None
    return config

# --- HÀM KẾT NỐI MT5 ---
def initialize_mt5(login, password, server):
    """Khởi tạo kết nối đến MetaTrader 5."""
    if not mt5.initialize(login=login, password=password, server=server, timeout=10000): # Timeout 10 giây
        logging.error(f"MT5 initialize() thất bại cho login {login}, mã lỗi = {mt5.last_error()}")
        mt5.shutdown()
        return False
    logging.info(f"MT5 đã khởi tạo thành công cho login {login}.")
    return True

# --- HÀM LẤY DỮ LIỆU TỪ MT5 ---
def get_account_info_mt5():
    """Lấy thông tin tài khoản (equity, balance)."""
    account_info = mt5.account_info()
    if account_info is None:
        logging.error(f"Không lấy được thông tin tài khoản, mã lỗi = {mt5.last_error()}")
        return None
    return {
        "accountEquity": account_info.equity,
        "accountBalance": account_info.balance
    }

def get_open_positions_mt5():
    """Lấy danh sách các lệnh đang mở."""
    positions = mt5.positions_get()
    if positions is None:
        logging.warning(f"Không lấy được danh sách lệnh đang mở, mã lỗi = {mt5.last_error()}. Có thể không có lệnh nào đang mở.")
        return []

    open_positions_data = []
    for pos in positions:
        # Đảm bảo thời gian được chuyển sang UTC và định dạng ISO 8601
        open_time_utc = datetime.fromtimestamp(pos.time, tz=timezone.utc).isoformat(timespec='seconds')
        
        # Xác định tradeType
        trade_type = "Unknown"
        if pos.type == mt5.ORDER_TYPE_BUY:
            trade_type = "Buy"
        elif pos.type == mt5.ORDER_TYPE_SELL:
            trade_type = "Sell"
            
        logging.info(f"Lấy được lệnh đang mở: {pos}")  

        open_positions_data.append({
            "eaTicketId": str(pos.ticket),
            "symbol": pos.symbol,
            "tradeType": trade_type,
            "volumeLots": pos.volume,
            "openPrice": pos.price_open,
            "openTime": open_time_utc,
            "currentMarketPrice": pos.price_current,
            "swap": pos.swap if hasattr(pos, 'swap') and pos.swap is not None else 0,
            "commission": pos.commission if hasattr(pos, 'commission') and pos.commission is not None else 0,
            "floatingPAndL": pos.profit
        })
    logging.info(f"Lấy được {len(open_positions_data)} lệnh đang mở.")
    return open_positions_data

def get_closed_trades_mt5(lookback_from_utc_timestamp):
    """
    Lấy lịch sử các deal đã đóng kể từ một thời điểm nhất định.
    Logic này cần được tinh chỉnh để nhóm các deal thành 'trades' hoàn chỉnh nếu cần.
    Ví dụ này sẽ lấy các deal đóng (out-deal).
    """
    to_date_utc_timestamp = int(datetime.now(timezone.utc).timestamp())
    deals = mt5.history_deals_get(lookback_from_utc_timestamp, to_date_utc_timestamp)

    if deals is None:
        logging.warning(f"Không lấy được history deals, mã lỗi = {mt5.last_error()}. Có thể không có deal nào trong khoảng thời gian này.")
        return []

    closed_trades_data = []
    processed_orders = set() # Để tránh xử lý trùng lặp lệnh gốc nếu có nhiều deal đóng 1 phần

    for deal in deals:
        # Chỉ quan tâm đến deal đóng vị thế (DEAL_ENTRY_OUT)
        # và đảm bảo chúng ta chưa xử lý lệnh gốc của deal này (nếu 1 lệnh được đóng bởi nhiều deal)
        if deal.entry == mt5.DEAL_ENTRY_OUT and deal.order not in processed_orders:
            # Tìm deal mở vị thế tương ứng dựa vào deal.position_id
            # Hoặc tìm order gốc nếu deal.order là ID của lệnh đóng
            # Đây là phần phức tạp cần logic chính xác để map deal thành "closed trade" theo DTO của bạn.
            # Ví dụ này sẽ đơn giản hóa bằng cách sử dụng thông tin từ deal đóng và cố gắng tìm deal mở.

            open_deal_info = None
            if deal.position_id != 0: # Hầu hết các deal đóng sẽ có position_id
                position_deals = mt5.history_deals_get(0, to_date_utc_timestamp) # Lấy tất cả deal để tìm deal mở
                if position_deals:
                    for pd in position_deals:
                        if pd.position_id == deal.position_id and pd.entry == mt5.DEAL_ENTRY_IN:
                            open_deal_info = pd
                            break
            
            if open_deal_info:
                # Xác định TradeType của lệnh GỐC (lệnh mở)
                original_order_type = "Unknown"
                if open_deal_info.type == mt5.DEAL_TYPE_BUY: # Deal mở là Buy -> lệnh gốc là Buy
                    original_order_type = "Buy"
                elif open_deal_info.type == mt5.DEAL_TYPE_SELL: # Deal mở là Sell -> lệnh gốc là Sell
                    original_order_type = "Sell"

                closed_trades_data.append({
                    "eaTicketId": str(deal.order), # Đây là ticket của LỆNH ĐÓNG (hoặc lệnh gốc nếu đóng 1 lần)
                                                  # Bạn có thể muốn dùng open_deal_info.order làm ticket gốc
                    "symbol": deal.symbol,
                    "tradeType": original_order_type, # Type của lệnh gốc
                    "volumeLots": deal.volume, # Volume của deal đóng này
                    "openPrice": open_deal_info.price,
                    "openTime": datetime.fromtimestamp(open_deal_info.time, tz=timezone.utc).isoformat(timespec='seconds'),
                    "closePrice": deal.price,
                    "closeTime": datetime.fromtimestamp(deal.time, tz=timezone.utc).isoformat(timespec='seconds'),
                    "swap": deal.swap if hasattr(deal, 'swap') and deal.swap is not None else 0, # Swap của deal này (MT5 có thể ghi swap/commission vào deal đóng)
                    "commission": deal.commission if hasattr(deal, 'commission') and deal.commission is not None else 0, # Commission của deal này
                    "realizedPAndL": deal.profit # Profit của deal này
                })
                processed_orders.add(deal.order) # Đánh dấu đã xử lý lệnh này để tránh trùng nếu có nhiều deal đóng 1 phần của cùng 1 order
                                                # Hoặc dùng deal.position_id nếu mỗi position đóng 1 lần.
            else:
                logging.warning(f"Không tìm thấy deal mở tương ứng cho deal đóng {deal.ticket} (position_id: {deal.position_id})")


    logging.info(f"Lấy được {len(closed_trades_data)} lệnh đã đóng.")
    return closed_trades_data

# --- HÀM GỌI API ---
def push_data_to_api(base_url, api_key, endpoint, trading_account_id, payload, disable_ssl_verification=False, timeout_seconds=30, max_retries=1, retry_delay_seconds=5):
    """Gửi dữ liệu JSON đến API endpoint với retry logic."""
    headers = {
        'Content-Type': 'application/json',
        'X-API-KEY': api_key # Header chứa API Key
    }
    full_url = f"{base_url}/trading-accounts/{trading_account_id}/{endpoint}"
    
    for attempt in range(max_retries + 1):  # +1 để bao gồm lần thử đầu tiên
        try:
            if attempt > 0:
                logging.info(f"Retry lần {attempt}/{max_retries} cho {full_url}")
                time.sleep(retry_delay_seconds)
                
            logging.info(f"Đang gửi request POST đến: {full_url} (timeout: {timeout_seconds}s)")
            if disable_ssl_verification:
                logging.warning("SSL verification đã bị tắt - chỉ dùng cho development!")
            # logging.debug(f"Headers: {headers}")
            # logging.debug(f"Payload: {json.dumps(payload, indent=2)}")

            # Tắt SSL verification nếu được yêu cầu (cho localhost với self-signed cert)
            verify_ssl = not disable_ssl_verification
            response = requests.post(full_url, headers=headers, data=json.dumps(payload), timeout=timeout_seconds, verify=verify_ssl)
            
            logging.info(f"Response từ {full_url} - Status: {response.status_code}")
            if response.status_code not in [200, 202]: # Chỉ chấp nhận 200 OK hoặc 202 Accepted
                logging.error(f"Lỗi khi gửi dữ liệu đến {full_url}. Status: {response.status_code}, Response: {response.text}")
                if attempt < max_retries:
                    continue  # Thử lại với HTTP error
                return False
            
            logging.info(f"Đã gửi dữ liệu thành công đến {full_url}. Response JSON: {response.json()}")
            return True
            
        except requests.exceptions.RequestException as e:
            logging.error(f"Lỗi request exception khi gửi dữ liệu đến {full_url} (attempt {attempt + 1}/{max_retries + 1}): {e}")
            if attempt < max_retries:
                continue  # Thử lại với exception
            return False
        except Exception as e:
            logging.error(f"Lỗi không xác định khi gửi dữ liệu đến {full_url} (attempt {attempt + 1}/{max_retries + 1}): {e}")
            if attempt < max_retries:
                continue  # Thử lại với exception
            return False
    
    return False  # Không bao giờ reach được đây nhưng để chắc chắn

# --- HÀM CHÍNH ---
def run_extractor():
    config = load_config()
    if not config:
        return

    api_base_url = config['General']['ApiBaseUrl']
    api_key = config['General']['ApiKey']
    log_level_str = config['General'].get('LogLevel', 'INFO').upper()
    lookback_hours_closed_trades = config['General'].getint('LookbackHoursClosedTrades', 24)
    disable_ssl_verification = config['General'].getboolean('DisableSSLVerification', False)
    request_timeout_seconds = config['General'].getint('RequestTimeoutSeconds', 30)
    max_retries = config['General'].getint('MaxRetries', 1)
    retry_delay_seconds = config['General'].getint('RetryDelaySeconds', 5)
    
    # Cập nhật log level từ config
    numeric_level = getattr(logging, log_level_str, None)
    if isinstance(numeric_level, int):
        logging.getLogger().setLevel(numeric_level)
    else:
        logging.warning(f"Log level '{log_level_str}' không hợp lệ. Sử dụng INFO mặc định.")

    for section in config.sections():
        if section.startswith("MT5Account_") and config.getboolean(section, 'Enabled', fallback=False):
            trading_account_id_system = config.getint(section, 'TradingAccountIdSystem')
            login = config.getint(section, 'Login')
            password = config.get(section, 'Password')
            server = config.get(section, 'Server')

            logging.info(f"Đang xử lý tài khoản: SystemID={trading_account_id_system}, MT5Login={login}")

            if not initialize_mt5(login, password, server):
                logging.error(f"Không thể khởi tạo MT5 cho login {login}. Bỏ qua tài khoản này.")
                continue

            # 1. Lấy và đẩy Live Data (Open Positions & Account Info)
            account_info = get_account_info_mt5()
            open_positions = get_open_positions_mt5()

            if account_info: # Chỉ đẩy nếu lấy được thông tin tài khoản
                live_data_payload = {
                    "accountEquity": account_info["accountEquity"],
                    "accountBalance": account_info["accountBalance"],
                    "openPositions": open_positions # open_positions có thể rỗng
                }
                push_data_to_api(f"{api_base_url}/ea-integration", api_key, "live-data", trading_account_id_system, live_data_payload, disable_ssl_verification, request_timeout_seconds, max_retries, retry_delay_seconds)
            else:
                logging.warning(f"Không có thông tin tài khoản cho MT5Login={login}, không thể đẩy live-data.")

            # 2. Lấy và đẩy Closed Trades
            # Tính thời điểm bắt đầu lấy lệnh đã đóng
            from_timestamp_utc = int((datetime.now(timezone.utc) - timedelta(hours=lookback_hours_closed_trades)).timestamp())
            closed_trades = get_closed_trades_mt5(from_timestamp_utc) # Cần logic hoàn thiện hơn
            
            if closed_trades: # Chỉ đẩy nếu có lệnh đã đóng
                closed_trades_payload = {"closedTrades": closed_trades}
                push_data_to_api(f"{api_base_url}/ea-integration", api_key, "closed-trades", trading_account_id_system, closed_trades_payload, disable_ssl_verification, request_timeout_seconds, max_retries, retry_delay_seconds)
            else:
                logging.info(f"Không có lệnh đã đóng mới nào để đẩy cho MT5Login={login} trong {lookback_hours_closed_trades} giờ qua.")

            mt5.shutdown()
            logging.info(f"Đã tắt MT5 cho login {login}.")
            time.sleep(1) # Chờ một chút giữa các tài khoản nếu có nhiều

# --- VÒNG LẶP CHÍNH ---
if __name__ == "__main__":
    general_config = load_config()
    if general_config:
        time_interval_seconds = general_config['General'].getint('TimeIntervalSeconds', 3)
        if time_interval_seconds < 1:
            logging.warning("TimeIntervalSeconds quá nhỏ (<1). Đặt lại thành 1 giây.")
            time_interval_seconds = 1
            
        logging.info(f"Script sẽ chạy mỗi {time_interval_seconds} giây.")
        while True:
            run_extractor()
            logging.debug(f"Hoàn thành lượt chạy. Nghỉ {time_interval_seconds} giây...")
            time.sleep(time_interval_seconds)
    else:
        logging.error("Không thể tải cấu hình. Script sẽ không chạy.")