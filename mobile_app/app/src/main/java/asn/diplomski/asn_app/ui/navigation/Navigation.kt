package asn.diplomski.asn_app.ui.navigation

import androidx.compose.foundation.layout.padding
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.filled.Build
import androidx.compose.material.icons.filled.Home
import androidx.compose.material.icons.filled.Person
import androidx.compose.material3.Icon
import androidx.compose.material3.NavigationBar
import androidx.compose.material3.NavigationBarItem
import androidx.compose.material3.Scaffold
import androidx.compose.material3.Text
import androidx.compose.runtime.Composable
import androidx.compose.runtime.getValue
import androidx.compose.ui.Modifier
import androidx.compose.ui.graphics.vector.ImageVector
import androidx.navigation.NavDestination.Companion.hierarchy
import androidx.navigation.NavGraph.Companion.findStartDestination
import androidx.navigation.compose.NavHost
import androidx.navigation.compose.composable
import androidx.navigation.compose.currentBackStackEntryAsState
import androidx.navigation.compose.rememberNavController
import asn.diplomski.asn_app.ui.auth.AuthRoute
import asn.diplomski.asn_app.ui.devices.DevicesRoute
import asn.diplomski.asn_app.ui.profile.ProfileRoute
import asn.diplomski.asn_app.ui.provisioning.ProvisioningRoute

sealed class Route(val route: String) {
    data object Auth : Route("auth")
    data object Main : Route("main")
    data object Devices : Route("devices")
    data object Provisioning : Route("provisioning")
    data object Profile : Route("profile")
}

private data class BottomNavItem(
    val route: Route,
    val label: String,
    val icon: ImageVector
)

@Composable
fun Navigation() {
    val rootNavController = rememberNavController()

    NavHost(navController = rootNavController, startDestination = Route.Auth.route) {
        composable(Route.Auth.route) {
            AuthRoute(
                onNavigateToDevices = {
                    rootNavController.navigate(Route.Main.route) {
                        popUpTo(Route.Auth.route) { inclusive = true }
                    }
                }
            )
        }
        composable(Route.Main.route) {
            MainScaffold(
                onNavigateToAuth = {
                    rootNavController.navigate(Route.Auth.route) {
                        popUpTo(0) { inclusive = true }
                    }
                }
            )
        }
    }
}

@Composable
private fun MainScaffold(onNavigateToAuth: () -> Unit) {
    val navController = rememberNavController()
    val navBackStackEntry by navController.currentBackStackEntryAsState()
    val currentDestination = navBackStackEntry?.destination

    val tabs = listOf(
        BottomNavItem(Route.Devices, "Devices", Icons.Default.Home),
        BottomNavItem(Route.Provisioning, "Provisioning", Icons.Default.Build),
        BottomNavItem(Route.Profile, "Profile", Icons.Default.Person)
    )

    Scaffold(
        bottomBar = {
            NavigationBar {
                tabs.forEach { tab ->
                    NavigationBarItem(
                        selected = currentDestination?.hierarchy?.any { it.route == tab.route.route } == true,
                        onClick = {
                            navController.navigate(tab.route.route) {
                                popUpTo(navController.graph.findStartDestination().id) {
                                    saveState = true
                                }
                                launchSingleTop = true
                                restoreState = true
                            }
                        },
                        icon = { Icon(tab.icon, contentDescription = tab.label) },
                        label = { Text(tab.label) }
                    )
                }
            }
        }
    ) { padding ->
        NavHost(
            navController = navController,
            startDestination = Route.Devices.route,
            modifier = Modifier.padding(padding)
        ) {
            composable(Route.Devices.route) {
                DevicesRoute(onNavigateToAuth = onNavigateToAuth)
            }
            composable(Route.Provisioning.route) {
                ProvisioningRoute()
            }
            composable(Route.Profile.route) {
                ProfileRoute(onNavigateToAuth = onNavigateToAuth)
            }
        }
    }
}
