use log::{error,info,warn};
use proxy_wasm::{
    traits::{Context,HttpContext,RootContext},
    types::{Action,ContextType,LogLevel}};
use serde::Deserialize;
use std::time::Duration;

const PATH: &str = ":path";
const HEADER_NAME: &str = "X-Load-Feedback";

// root handler holds config
struct RootHandler {
    config: FilterConfig,
}

// each request gets burst_header and path from root
pub struct RequestContext {
    add_header: bool,
    burst_header: String,
    path: String,
}

// config structure
#[derive(Deserialize, Debug)]
#[serde(default)]
pub struct FilterConfig {
    /// current burst header - gets updated on time
    burst_header: String,

    /// Cache duration in seconds
    cache_seconds: u64,

    /// Name of this deployment
    deployment: String,

    /// Namespace of this app
    namespace: String,

    /// The authority to set when calling the HTTP service providing headers.
    service_authority: String,

    /// The Envoy cluster name
    service_cluster: String,

    /// The path to call on the HTTP service providing headers.
    service_path: String,

    /// path
    path: String,
}

// default values for config
impl Default for FilterConfig {
    fn default() -> Self {
        FilterConfig {
            burst_header: String::new(),
            cache_seconds: 60 * 60 * 24,
            deployment: String::new(),
            namespace: String::new(),
            service_authority: String::new(),
            service_cluster: String::new(),
            service_path: String::new(),
            path: String::new(),
        }
    }
}

#[no_mangle]
pub fn _start() {
    proxy_wasm::set_log_level(LogLevel::Warn);

    info!("starting lab 4");

    // create root context and load config
    proxy_wasm::set_root_context(|_context_id| -> Box<dyn RootContext> {
        Box::new(RootHandler { config: FilterConfig::default() })
    });
}

// Root Context implementation

impl Context for RootHandler {
    // async http request callback from metrics service
    fn on_http_call_response(
        &mut self,
        _token_id: u32,
        _num_headers: usize,
        body_size: usize,
        _num_trailers: usize,
    ) {
        // get the response body of metrics server call
        match self.get_http_call_response_body(0, body_size) {
            Some(body) => {
                // Store the header in self.config
                self.config.burst_header = String::from_utf8(body.clone()).unwrap();
            },
            None => {
                // log an error
                error!("header providing service returned empty body");
            }
        };
    }
}

impl RootContext for RootHandler {
    // create http context for new requests
    fn create_http_context(&self, _context_id: u32) -> Option<Box<dyn HttpContext>> {
        Some(Box::new(RequestContext {
            add_header: false,
            // copy the values from root config to request
            path: self.config.path.clone(),
            burst_header: self.config.burst_header.clone(),
        }))
    }

    // required for create_http_context to work
    fn get_type(&self) -> Option<ContextType> {
        Some(ContextType::HttpContext)
    }

    // read the config and store in self.config
    fn on_configure(&mut self, _config_size: usize) -> bool {
        match self.get_configuration() {
            Some(c) => {
                // Parse and store the configuration
                match serde_json::from_slice::<FilterConfig>(c.as_ref()) {
                    Ok(config) => {
                        self.config = config;
                    }
                    Err(e) => {
                        // fail on invalid config
                        error!("failed to parse configuration: {:?}", e);
                        return false;
                    }
                }
            }
            None => {
                // fail on missing config
                error!("configuration missing");
                return false;
            }
        };

        // set a short duration to cause the timer to fire quickly
        self.set_tick_period(Duration::from_secs(1));

        return true;
    }

    // refresh the cache on a timer
    fn on_tick(&mut self) {
        self.set_tick_period(Duration::from_secs(self.config.cache_seconds));

        // dispatch an async HTTP call to the configured cluster
        // response is handled in Context::on_http_call_response
        let res = self.dispatch_http_call(
            &self.config.service_cluster,
            vec![
                (":method", "GET"),
                (":path", &format!("{}/{}/{}", self.config.service_path, self.config.namespace, self.config.deployment)),
                (":authority", &self.config.service_authority),
            ],
            None,
            vec![],
            Duration::from_secs(5),
        );

        match res {
            Err(e) =>{
                warn!("metrics service request failed: {:?}", e);

                // retry quickly
                self.set_tick_period(Duration::from_secs(2));
                }
            Ok(_)  => {}
        }        
    }
}

// http request implemenetation

// nothing implemented
impl Context for RequestContext {}

impl HttpContext for RequestContext {

    // check headers for path match and store in self
    fn on_http_request_headers(&mut self, _: usize) -> Action {

        if self.get_http_request_header(PATH).unwrap_or_default() == self.path {
            self.add_header = true;
        }

        Action::Continue
    }
    
    // add the header if path matched
    fn on_http_response_headers(&mut self, _num_headers: usize) -> Action {

        if self.add_header {
            self.set_http_response_header(HEADER_NAME,Some(&self.burst_header));
        }

        Action::Continue
    }
}
