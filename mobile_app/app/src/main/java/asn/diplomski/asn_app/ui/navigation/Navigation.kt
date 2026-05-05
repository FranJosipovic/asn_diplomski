package asn.diplomski.asn_app.ui.navigation

import androidx.compose.runtime.Composable
import androidx.navigation.NavHostController
import androidx.navigation.compose.NavHost
import androidx.navigation.compose.composable
import androidx.navigation.compose.rememberNavController
import asn.diplomski.asn_app.ui.auth.AuthRoute
import asn.diplomski.asn_app.ui.devices.DevicesRoute

sealed class Route(val route: String) {
    data object Auth : Route("auth")
    data object Devices : Route("devices/{tenantId}") {
        fun createRoute(tenantId: Long) = "devices/$tenantId"
    }
}

@Composable
fun Navigation() {
    val navController = rememberNavController()

    NavHost(
        navController = navController,
        startDestination = Route.Auth.route
    ) {
        composable(Route.Auth.route) {
            AuthRoute(
                onNavigateToDevices = { tenantId ->
                    navController.navigate(Route.Devices.createRoute(tenantId)) {
                        popUpTo(Route.Auth.route) { inclusive = true }
                    }
                }
            )
        }

        composable(Route.Devices.route) { backStackEntry ->
            val tenantId = backStackEntry.arguments?.getString("tenantId")?.toLongOrNull() ?: 0L
            DevicesRoute(tenantId = tenantId)
        }
    }
}
