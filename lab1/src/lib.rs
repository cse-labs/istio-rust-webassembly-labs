use log::{info};
use proxy_wasm::{
    traits::{Context,HttpContext,RootContext},
    types::{Action,ContextType,LogLevel}};

const HEADER_NAME: &str = "X-Wasm";
const HEADER: &str = "Hello from WebAssembly!";

#[no_mangle]
pub fn _start() {
    proxy_wasm::set_log_level(LogLevel::Info);

    info!("starting lab 1");

    // create root context and load config
    proxy_wasm::set_root_context(|_context_id| -> Box<dyn RootContext> {
        Box::new(RootHandler { })
    });
}

// root handler holds config
struct RootHandler {}

// Root Context implementation

impl Context for RootHandler {}

impl RootContext for RootHandler {
    // create http context for new requests
    fn create_http_context(&self, _context_id: u32) -> Option<Box<dyn HttpContext>> {
        Some(Box::new(RequestHandler { }))
    }

    // required for create_http_context to work
    fn get_type(&self) -> Option<ContextType> {
        Some(ContextType::HttpContext)
    }
}

// http request implemenetation

// each request gets burst_header and user_agent from root
struct RequestHandler {}

// nothing implemented
impl Context for RequestHandler {}

impl HttpContext for RequestHandler {

    // add the header
    fn on_http_response_headers(&mut self, _num_headers: usize) -> Action {

        self.set_http_response_header(HEADER_NAME,Some(HEADER));

        Action::Continue
    }
}
